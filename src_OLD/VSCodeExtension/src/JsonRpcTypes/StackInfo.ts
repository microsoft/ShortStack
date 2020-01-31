/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC to say hello world to the caller.
 */
namespace JsonRpcTypes {
    export interface StackInfo {
        /**
         * User-Chosen Stack name
         */
        name: string;
        
        /**
         * All available levels in this stack
         */
        levels: StackLevel[];

        /**
         * The number of the current level (not necessarily the index in levels)
         */
        currentLevelNumber: number | undefined;

        /**
         * Url to the source repository
         */
        repositoryUrl: string;
        
        /**
         * The root of the repository.
         */
        repositoryRootPath: string;
    }
}