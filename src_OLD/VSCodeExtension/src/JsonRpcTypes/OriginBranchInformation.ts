/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC to say hello world to the caller.
 */
namespace JsonRpcTypes {
    export interface OriginBranchInformation {
        /**
         * Gets the friendly name of the branch.
         */
        friendlyName : string;

        /**
         * Gets the remote name of the branch.
         */
        remoteName : string;

        /**
         * Gets the canonical name of the branch.
         */
        canonicalName : string;
    }
}