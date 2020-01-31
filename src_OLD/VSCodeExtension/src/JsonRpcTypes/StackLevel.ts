/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

namespace JsonRpcTypes {
    export interface StackLevel {
        /**
         * The number of this level
         */
        number: number;

        /**
         * 
         */
        isCurrent: boolean;

        /**
         * Name of the stack for this level
         */
        stackName: string;

        /**
         * The branch to pull from
         */
        originBranch: string;

        /**
         * The branch to push to
         */
        targetOriginBranch: string;

        /**
         * The branch on this PC
         */
        localbranch: string;

        /**
         * names of all the local changed files
         */
        changedFiles: string[];

        /**
         * names of all the local added files
         */
        addedFiles: string[];

        /**
         * names of all the local removed files
         */
        removedFiles: string[];

        /**
         * Ids of commits that have not been pushed to TargetOriginBranch
         */
        unpushedCommitIds: string[];

        /**
         * Ids of commits that have not been pulled from OriginBranch 
         */
        unpulledCommitIds: string[];

        /**
         * Pull request for this stack level.  (Created by ss-push)
         */
        pullRequest: StackPullRequest;
    }
}