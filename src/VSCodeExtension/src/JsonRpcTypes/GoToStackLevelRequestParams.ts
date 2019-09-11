/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface GoToStackLevelRequestParams {
        /**
         * The stack witch contains the level to switch to.
         */
        stackInfo : StackInfo;

        /**
         * The stack level to switch to.
         */
        stackLevel: StackLevel;
    }
}