/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

 namespace JsonRpcTypes {
    export interface StackPullRequestComment {
        /**
         * Content of this comment
         */
        content: string;

        /**
         * Author name of this comment
         */
        author: string;

        /***
         * Date of the comment
         */
        date: Date;
    }
}