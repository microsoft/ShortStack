
export class GitCommit
{
    Message: string | undefined;

}

export class GitBranchInfo
{
    RemoteName: string | undefined;
    UpstreamBranchCanonicalName: any;
    IsCurrentRepositoryHead = false;
    GetMostRecentCommit(): GitCommit {
        throw new Error("Method not implemented.");
    }
    name: string | undefined;   
    FriendlyName: any;
}

export interface IGitHelper
{
    RemoteUrl: string;
    CurrentBranch:string;
    RepositoryRootPath: string;
    HasUncommittedChanges: boolean;
    LocalBranches: GitBranchInfo[];
}

export class GitHelper implements IGitHelper
{
    get CurrentBranch() {return ""}
    get RepositoryRootPath() {return ""}

    get HasUncommittedChanges() {
        return false;
    }   

    get LocalBranches() { return []}

}
