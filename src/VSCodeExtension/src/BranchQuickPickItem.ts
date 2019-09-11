/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

'use strict';

import * as vscode from 'vscode';

export class BranchQuickPickItem implements vscode.QuickPickItem {

    branch: JsonRpcTypes.OriginBranchInformation;
    label: string;
    description: string;
    picked: boolean;

    constructor(branch : JsonRpcTypes.OriginBranchInformation) {
        this.branch = branch;
        this.label = branch.friendlyName;
        this.description = branch.canonicalName;
        this.picked = false;
    }
}
