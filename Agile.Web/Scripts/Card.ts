/// <reference path="../../framework/signum.web/signum/scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function unarchive(options: Operations.OperationOptions, findOptions: Finder.FindOptions, url: string): Promise<void> {

    options.controllerUrl = url;

    return Finder.find(findOptions).then(list=> {

        if (list)// could return null, but we let it continue 
            options.requestExtraJsonData = { list: list.key() };

        return Operations.executeDefault(options);
    });
}