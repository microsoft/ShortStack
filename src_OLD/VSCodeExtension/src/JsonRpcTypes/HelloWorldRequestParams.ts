/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 /**
 * Used by the JSON-RPC Layer to receive the caller's name.
 */
namespace JsonRpcTypes {
    export interface HelloWorldRequestParams {
        /**
         * Gets or sets your name.
         */
        yourName : string;
    }
}