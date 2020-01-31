/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface PushStackLevelRequestParams {
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