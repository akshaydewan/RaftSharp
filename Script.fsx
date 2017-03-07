module Raft =
    // -------------
    // Types
    type LogEntry = {
        term: int
        index: int
    }
    type ElectionTimeout = string
    type VoteRequest = { 
        candidateId: int
        term: int 
        lastLogIndex: int
        lastLogTerm: int
    }
    type Heartbeat = {
        term: int
        leaderId: int
        prevLogIndex: int
        prevLogTerm: int
        leaderCommitIndex: int
    }
    type MajorityVotesReceived = string
    type ClientRequest = string
    type MajorityAppended = string
    type Append = string
    type AppendRejected = string
    type HeartbeatTimeout = string
    type Event = 
        | ElectionTimeout of ElectionTimeout
        | VoteRequest of VoteRequest 
        | MajorityVotesReceived of MajorityVotesReceived
        | ClientRequest of ClientRequest
        | Append of Append
        | AppendRejected of AppendRejected
        | HeartbeatTimeout of HeartbeatTimeout

    type VoteResponse = 
        | Vote
        | NoVote

    type Action = 
        | RequestVoteAction of VoteRequest
        | SendVoteResponse of VoteResponse
        | SendHeartbeat of Heartbeat
        | NoAction

    type ServerState = {
        id: int
        currentTerm: int
        votedFor: int option
        log: seq<LogEntry>
    }
    type Leader = {serverState:ServerState}
    type Candidate = {serverState:ServerState}            
    type Follower = {serverState:ServerState}
    type Server = 
        | Leader of Leader
        | Follower of Follower
        | Candidate of Candidate


    //--------------------------------------------------------------------
    // Helper functions
    let incrementTerm serverState = { 
        id = serverState.id
        currentTerm = serverState.currentTerm + 1;
        votedFor = serverState.votedFor
        log = serverState.log
    }

    let voteForSelf serverState = {
        id = serverState.id
        currentTerm = serverState.currentTerm;
        votedFor = Some serverState.id
        log = serverState.log
    }

    let lastLogIndex serverState = (Seq.head serverState.log).index
    let lastLogTerm serverState = (Seq.head serverState.log).term


    //--------------------------------------------------------------------
    // State Machine helper functions
    let createVoteRequest serverState = Action.RequestVoteAction{
        candidateId = serverState.id
        term = serverState.currentTerm
        lastLogIndex = lastLogIndex serverState
        lastLogTerm = lastLogTerm serverState
    }

    let promoteToCandidate serverState =  {
        Candidate.serverState = incrementTerm serverState |> voteForSelf
    }
    let isEligibleForVote (voteRequest:VoteRequest, serverState) = 
        voteRequest.term >= serverState.currentTerm &&
        serverState.votedFor.IsNone &&
        voteRequest.lastLogIndex >= lastLogIndex serverState &&
        voteRequest.lastLogTerm >= lastLogTerm serverState

    let getVoteResponse (voteRequest:VoteRequest, serverState) =  
        if isEligibleForVote (voteRequest, serverState) then Vote else NoVote

    let establishAsLeader serverState = SendHeartbeat {
        leaderId = serverState.id
        term = serverState.currentTerm
        prevLogIndex = lastLogIndex serverState
        prevLogTerm = lastLogTerm serverState
        leaderCommitIndex = 0 //TODO
    }

    //-------------------------------------------------------------------- 
    // Message Handlers
    let handleElectionTimeout server = 
        match server with
        | Follower {serverState = ss} -> 
            let candidate = promoteToCandidate ss
            Server.Candidate candidate, createVoteRequest candidate.serverState
        | Candidate {serverState = ss} -> 
            let candidate = promoteToCandidate ss
            Server.Candidate candidate, createVoteRequest candidate.serverState
        | Leader _ -> server, NoAction

    let handleVoteRequest (voteRequest, server) =
        match server with
        | Follower {serverState = ss} -> 
            Server.Follower {serverState = ss}, getVoteResponse (voteRequest, ss)
        | Candidate {serverState = ss} ->
            let voteResponse = getVoteResponse (voteRequest, ss)
            match voteResponse with
            | Vote -> Server.Follower {serverState = ss}, Vote
            | NoVote -> Server.Candidate {serverState = ss}, NoVote
        | Leader {serverState = ss} ->
            let voteResponse = getVoteResponse (voteRequest, ss)
            match voteResponse with
            | Vote -> Server.Follower {serverState = ss}, Vote
            | NoVote -> Server.Leader {serverState = ss}, NoVote
    
    let handleMajorityVotesReceived server =
        match server with
        | Follower {serverState = serverState} -> Follower {serverState = serverState}, NoAction
        | Candidate {serverState = serverState} -> Leader {serverState = serverState}, establishAsLeader serverState
        | Leader {serverState = serverState} -> Leader {serverState = serverState}, NoAction
    
    // let handleClientRequest server = ()
    // let handleAppend server = ()
    // let handleAppendRejected server = ()
    // let handleHeartbeatTimeout server = ()


(*
    let handleMessageToServer (message:Message) (server:Server):server = 
        match message with
        | ElectionTimeout ->
            handleElectionTimeout server
        | VoteRequest ->
            handleVoteRequest server
        | MajorityVotesReceived ->
            handleMajorityVotesReceived server
        | ClientRequest ->
            handleClientRequest server
        | Append ->
            handleAppend server
        | AppendRejected ->
            handleAppendRejected server
        | HeartbeatTimeout ->
            handleHeartbeatTimeout server
*)