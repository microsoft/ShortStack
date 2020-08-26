import { RestHelper } from "./RestHelper";

export interface GitTag
{
    name: string;
    zipball_url: string;
    tarball_url: string;
    commit: { sha: string; url: string; };
    node_id: string;
}

export interface GitNodeData
{
  name: string;
  path: string;
  sha: string;
  size: number;
  url: string;
  html_url: string;
  git_url: string;
  download_url: string | null;
  type: string;
  submodule_git_url: string;
  _links: {
      self: string;
      git: string;
      html: string;
  }
}

//------------------------------------------------------------------------------
// Convenient Factory for getting git objects
//------------------------------------------------------------------------------
export class GitFactory
{
    server: string;
    apiToken: string;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor (server: string, apiToken: string)
    {
        this.server = server;
        this.apiToken = apiToken;
    }

    //------------------------------------------------------------------------------
    // get a repo object for a specific repo
    //------------------------------------------------------------------------------
    getRemoteRepo(repoOwner: string, repoName: string)
    {
        return new GitRemoteRepo(this.server, repoOwner, repoName, this.apiToken);
    }
}

//------------------------------------------------------------------------------
// Access git repository
// see https://developer.github.com/v3/
//------------------------------------------------------------------------------
export class GitRemoteRepo
{
    owner: string;
    name: string;
    apiToken: string;
    cloneUrl: string;
    _graphQLApi: RestHelper;
    _gitApiHelper: RestHelper;

    //------------------------------------------------------------------------------
    // ctor
    //------------------------------------------------------------------------------
    constructor (server: string, repoOwner: string, repoName: string, apiToken: string)
    {
        this.cloneUrl = `git@${server}:${repoOwner}/${repoName}.git`
        this.name = repoName;
        this.owner = repoOwner;
        this.apiToken = apiToken;

        this._graphQLApi = new RestHelper(`https://${server}/api/graphql`)
        this._graphQLApi.addHeader("Authorization", "token " + this.apiToken);

        this._gitApiHelper = new RestHelper(`https://${server}/api/v3/repos/${repoOwner}/${repoName}`);
        this._gitApiHelper.addHeader("Authorization", "token " + this.apiToken);
    }

    //------------------------------------------------------------------------------
    // Get tags on this reqpo
    //------------------------------------------------------------------------------
    async findTag(regex: RegExp): Promise<GitTag | undefined>
    {
        for(let page = 0; page < 1000; page++)
        {
            const result = await this._gitApiHelper.restGet<GitTag[]>("/tags?per_page=100&page=" + page);
            if(!result) break;
            for(const tag of result)
            {
                if(regex.exec(tag.name)) return tag;
            }
            if(result.length < 100) break;
        }
        return undefined;
    }

    //------------------------------------------------------------------------------
    // Return git content details for a file
    //------------------------------------------------------------------------------
    async getFile(branchOrSha: string, path: string): Promise<GitNodeData>
    {
        if(!path.startsWith("/")) path = "/" + path;

        return await this._gitApiHelper.restGet<GitNodeData>(`/contents${path}?ref=${branchOrSha}&per_page=1`);
    }

    //------------------------------------------------------------------------------
    // Return git content details for a directory
    //------------------------------------------------------------------------------
    async getNodeChildren(branchOrSha: string, path: string): Promise<GitNodeData[]>
    {
        const output = new Array<GitNodeData>();
        if(!path.startsWith("/")) path = "/" + path;

        for(let page = 0; page < 1000; page++)
        {
            const result = await this._gitApiHelper.restGet<GitNodeData[]>(`/contents${path}?ref=${branchOrSha}&per_page=100&page=` + page);
            if(!result) break;
            for(const item of result)
            {
                output.push(item);
            }
            if(result.length < 100) break;
        }
        return output;
    }

    //------------------------------------------------------------------------------
    // Get text contents
    //------------------------------------------------------------------------------
    async getTextContents(branchOrSha: string, relativePath: string)
    {
        const query = `query {
            repository(name: ${JSON.stringify(this.name)}, owner: ${JSON.stringify(this.owner)}) {
              object(expression: ${JSON.stringify(branchOrSha + ":" + relativePath)}) {
                    ... on Blob {
                      text
                    }
                  }
            }
          }
        `;
        //console.log(query);
        const result = await this._graphQLApi.restPost<{data: {repository: {object: {text: string}}}}>("", JSON.stringify({query}))
        //console.log(JSON.stringify(result));
        return result.data.repository.object.text;
    }

    //------------------------------------------------------------------------------
    // Return the first commit with a message that matches the regex
    // path: specify this to limit the commit search to a certain file
    //------------------------------------------------------------------------------
    async processCommitHistory( 
        startPath: string | undefined, 
        processCommit: (commit: {committedDate: string, message: string, oid: string}) => boolean)
    {
        const queryTemplate = `query {
            repository(name: ${JSON.stringify(this.name)}, owner: ${JSON.stringify(this.owner)}) {
                defaultBranchRef {
                    target {
                      ... on Commit {
                        history(##HISTORY_PARAMS##) {
                          edges {
                            node {
                              committedDate
                              message
                              oid
                            }
                          }
                        }
                      }
                    }
                  }
                }
            }
        `;

        let lastCommitTimestamp: string | undefined; 
        const pageSize = 50;

        // Search through commit history 
        while(true)
        {
            let historyPart = `first: ${pageSize}`;
            if(startPath && startPath !== "") historyPart += `, path: ${JSON.stringify(startPath)}`;
            if(lastCommitTimestamp) historyPart += `, until: ${JSON.stringify(lastCommitTimestamp)}`
            const query = queryTemplate.replace("##HISTORY_PARAMS##", historyPart)
            const result = await this._graphQLApi.restPost<any>("", JSON.stringify({query}))
            let count = 0;

            for(const edge of result.data.repository.defaultBranchRef.target.history.edges)
            {
                if(edge.node.committedDate === lastCommitTimestamp) continue;
                count++;
                if(!processCommit(edge.node)) return;
                lastCommitTimestamp = edge.node.committedDate;
            }

            if(!count) break;
        }
    }
}