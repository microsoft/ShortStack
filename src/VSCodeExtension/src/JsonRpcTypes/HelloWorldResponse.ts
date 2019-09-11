/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC to say hello world to the caller.
 */
namespace JsonRpcTypes {
    export interface HelloWorldResponse {
        /**
         * Gets or sets the hello world response.
         */
        helloWorld : string;
    }
}