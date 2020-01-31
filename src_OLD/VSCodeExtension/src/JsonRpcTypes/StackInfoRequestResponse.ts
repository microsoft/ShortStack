/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC to say hello world to the caller.
 */
namespace JsonRpcTypes {
    export interface StackInfoRequestResponse {
        /**
         * The response to the StackInfoRequest.
         */
        stackInfo: StackInfo[];
    }
}