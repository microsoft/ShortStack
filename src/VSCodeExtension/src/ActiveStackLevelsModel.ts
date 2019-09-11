/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All Rights Reserved.
 * ------------------------------------------------------------------------------------------ */

'use strict';

import * as vscode from 'vscode';
import * as path from 'path';

/**
 * Contains the data model held for a single tree-view item for a single stack level.
 */
export class ActiveStackLevelsModel extends vscode.TreeItem {
    readonly stackLevel: JsonRpcTypes.StackLevel;
    extensionContext: vscode.ExtensionContext;

    constructor(stackLevel: JsonRpcTypes.StackLevel, extensionContext: vscode.ExtensionContext) {
        /**
         * The "None" collapse state indicates that we do not have any children.
         */
        super(stackLevel.stackName + " " + stackLevel.number, vscode.TreeItemCollapsibleState.None);
        this.extensionContext = extensionContext;
        this.stackLevel = stackLevel;
        const iconPath : string | undefined = stackLevel.isCurrent ? extensionContext.asAbsolutePath(path.join("resources", "currentLevel.svg")) : undefined;

        if (iconPath) {
            this.iconPath = {
                light: iconPath,
                dark: iconPath
            };    
        }

        /**
         * The context value is used by VSCode to know what context-menu items are available for tree-view items of this type.
         */
        this.contextValue = stackLevel.isCurrent ? "currentStackLevel" : "stackLevel";
    }
}
