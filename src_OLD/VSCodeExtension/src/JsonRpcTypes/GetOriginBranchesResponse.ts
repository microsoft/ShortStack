/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface GetOriginBranchesResponse {
        /**
         * Gets the origin branches.
         */
        branches : OriginBranchInformation[];
    }
}