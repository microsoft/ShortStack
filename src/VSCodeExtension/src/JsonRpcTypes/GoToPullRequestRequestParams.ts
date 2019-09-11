/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface GoToPullRequestRequestParams {
        /**
         * The stack witch contains the level to switch to.
         */
        stackInfo : StackInfo;

        /**
         * The stack level to view the pull request information.
         */
        stackLevel: StackLevel;
    }
}