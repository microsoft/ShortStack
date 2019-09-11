/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

'use strict';

import * as vscode from 'vscode';
import { ActiveStackLevelsModel } from './ActiveStackLevelsModel';

/**
 * Contains the data model held by the tree-view item for a single stack.
 */
export class ActiveStackModel extends vscode.TreeItem {
    modelChildren : ActiveStackLevelsModel[]  = [];
    extensionContext: vscode.ExtensionContext;

    /**
     * Contains the information for this stack.
     */
    readonly stackInformation: JsonRpcTypes.StackInfo;

    /**
     * Constructs an @see ActiveStackModel object.
     * @param stackInformation The stack information to hold for this stack.
     */
    constructor(stackInformation: JsonRpcTypes.StackInfo, extensionContext: vscode.ExtensionContext) {
        /**
         * The "None" collapse state indicates that we do not have any children.
         */
        super(stackInformation.name, (stackInformation.levels && stackInformation.levels.length !== 0) ?   vscode.TreeItemCollapsibleState.Collapsed : vscode.TreeItemCollapsibleState.None);
        this.stackInformation = stackInformation;
        this.extensionContext = extensionContext;

        /**
         * The context value is used by VSCode to know what context-menu items are available for tree-view items of this type.
         */
        this.contextValue = "activeStack";
    }

    getChildren(element?: ActiveStackLevelsModel): vscode.ProviderResult<ActiveStackLevelsModel[]> {
        return new Promise((resolve, reject) => {
            if (!this.modelChildren || this.modelChildren.length === 0) {
                for (const level of  this.stackInformation.levels) {
                    this.modelChildren.push(new ActiveStackLevelsModel(level, this.extensionContext));
                } 
            }

            resolve(this.modelChildren);
        });
    }
}
