

export interface IGitHelper
{
    HasUncommittedChanges: boolean
}

export class GitHelper implements IGitHelper
{
    get HasUncommittedChanges() {
        return false;
    }   
}
