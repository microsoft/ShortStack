/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC to say hello world to the caller.
 */
namespace JsonRpcTypes {
    export interface StackInfoRequestParams {
        /**
         * This could be a folder or a file name.
         */
        editorPath: string;
    }
}