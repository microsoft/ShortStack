import fetch from 'cross-fetch';

// -------------------------------------------------------------------
// Interface for storing locally somehow
// -------------------------------------------------------------------
export interface ILocalStorage {
    loadObject: <T>(key: string) => Promise<T | null>;
    saveObject: (key: string, saveMe: object) => Promise<void>;
    removeObject: (key: string) => Promise<void>;
    clear: () => Promise<void>;
};

//------------------------------------------------------------------------------
// HttpResponse definition
//------------------------------------------------------------------------------
interface HttpResponse
{
    status: number;
    statusText: string;
    headers: any;
    url: string;
    text: () => Promise<string>
}

//------------------------------------------------------------------------------
// RestHelper interface - useful for testing
//------------------------------------------------------------------------------
export interface IRestHelper
{
    apiRoot: string;
    addHeader: (name: string, value:string) => void;
    restGet: <T>(query: string) => Promise<T>;
    restGetText: (query: string) => Promise<string>;
    restPost: <T>(query: string, jsonBody:string) => Promise<T>;
    removeFromCache: (query: string) => Promise<void>;
}

//------------------------------------------------------------------------------
// Class to assist with REST calling
//------------------------------------------------------------------------------
export class RestHelper implements IRestHelper
{
    _headers: string[][] = [];
    apiRoot: string;
    _cache: ILocalStorage | undefined;

    // Prefix all http requests with this prefix text
    _callPrefix: string | undefined;

    //------------------------------------------------------------------------------
    // helper for rest calls
    //
    // callPrefix is used to append special text to the beginning of all request urls.
    // this is helpful for servers that use prefix text to reroute calls
    //------------------------------------------------------------------------------
    constructor(apiRoot: string, cache?: ILocalStorage, callPrefix: string = "")
    {
        this._cache = cache;
        this.apiRoot = apiRoot
        this._callPrefix = callPrefix;
    }

    //------------------------------------------------------------------------------
    // Add a header to use on all of the calls
    //------------------------------------------------------------------------------
    addHeader(name: string, value:string)
    {
        this._headers.push([name, value]);
    }

    //------------------------------------------------------------------------------
    // Attempt a json conversions and throw useful text if there is an error
    //------------------------------------------------------------------------------
    jsonConvert<T>(query:string, jsonBody: string)
    {
        try{ return JSON.parse(jsonBody) as T; }
        catch(err)
        {
            if(this._cache)
            {
                this._cache.removeObject(query);
            }
            throw new Error(`Non-Json body returned on ${this.apiRoot}${query}\nResponse: ${jsonBody}` ) ;
        }
    }

    //------------------------------------------------------------------------------
    // Get an object
    //------------------------------------------------------------------------------
    async restGet<T>(query: string): Promise<T> {
        return this.jsonConvert<T>(query, await this.restCall("GET", query, undefined));
    }

    //------------------------------------------------------------------------------
    // get a string
    //------------------------------------------------------------------------------
    async restGetText(query: string): Promise<string> {
        return await this.restCall("GET", query, undefined);
    }

    //------------------------------------------------------------------------------
    // helper for rest calls
    //------------------------------------------------------------------------------
    async restPost<T>(query: string, jsonBody:string): Promise<T> {
        return this.jsonConvert<T>(query, await this.restCall("POST", query, jsonBody));
    }
    
    //------------------------------------------------------------------------------
    // helper for rest calls
    //------------------------------------------------------------------------------
    async restCall(method: string, query: string, jsonBody:string | undefined): Promise<string> {
        const url = `${this._callPrefix}${this.apiRoot}${query}`;
        //console.log("URL: " + url);

        if(this._cache)
        {
            const cachedString = await this._cache.loadObject<{data: string}>(query);
            if(cachedString) return cachedString.data;
        }

        const request = { method: method, body: jsonBody, headers: this._headers };

        //console.log("REQUEST: " + JSON.stringify(request));

        return fetch(url, request)
            .then(async(response: HttpResponse) => {
                if(response.status === 301)
                {
                    throw new Error(`Got a 301 error.  The requesting URL (${url}) is wrong.  it should be: ${response.headers["location"]}`)
                } 
                
                if(response.status === 200 // OK
                    || response.status === 410  // Gone or empty.  for JSON replies, this means "{}"
                    )
                {
                    const text = await response.text();
                    if(this._cache) {
                        this._cache.saveObject(query, {data: text});
                    }
                    return text;
                }
                else {
                    throw Error(`Unexpected response: ${response.status}: ${await response.text()}`)
                } 
            })
            .catch((error: any) => {
                throw Error(`Error on URL: ${url}\n: ${error}`)
            });
    }

    //------------------------------------------------------------------------------
    // remove the item from the cache if it is there
    //------------------------------------------------------------------------------
    async removeFromCache (query: string) 
    {
        return this._cache?.removeObject(query);
    }
}