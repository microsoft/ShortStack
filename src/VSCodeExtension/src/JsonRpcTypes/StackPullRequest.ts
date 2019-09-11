/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface StackPullRequest {
        /**
         * Title of the stacked pull request.
         */
        title: string;

        /**
         * Description of the stacked pull request.
         */
        description: string;

        /**
         * State of the stacked pull request.
         */
        state: string;

        /**
         * Comments attached to the pull request.
         */
        comments: StackPullRequestComment[];
    }
}