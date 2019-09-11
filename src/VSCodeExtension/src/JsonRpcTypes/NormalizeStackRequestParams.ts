/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface NormalizeStackRequestParams {
        /**
         * The stack to switch to.
         */
        stackInfo : StackInfo;

        /**
         * True to normalize with origin.
         */
        normalizeWithOrigin? : boolean;
    }
}