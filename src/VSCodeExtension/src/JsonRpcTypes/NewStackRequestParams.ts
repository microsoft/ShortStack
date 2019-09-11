/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface NewStackRequestParams {
        /**
         * The path in which to create a new stack.
         */
        startPath : string;

        /**
         * The name of the new stack.
         */
        stackName: string;

        /**
         * The desired origin branch.
         */
        desiredOriginBranch: string;
    }
}