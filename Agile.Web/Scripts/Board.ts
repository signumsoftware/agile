/// <reference path="../../framework/signum.web/signum/scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function init() {
    var board = $("div.board");

    board.closest("container").remove("container").addClass("container-fluid");
}


export function initList(urlCreate: string) {

    var board = $("div.board");

    board.on("click", "a.list-add-card", e=> {
        $(e.currentTarget).hide();
        $(e.currentTarget).siblings(".list-create-card-block").show();
    }); 

    board.on("click", "a.list-create-card-cancel", e=> {
        var block = $(e.currentTarget).closest(".list-create-card-block");
        block.hide();
        block.siblings(".list-add-card").show();
    });
}

export function initCard() {
    var board = $("div.board");

    board.on("click", "div.card", e=> {

        var liteKey = $(e.currentTarget).attr("data-card");

        var eHtml = new Entities.EntityHtml("Card", Entities.RuntimeInfo.fromKey(liteKey));

        Navigator.viewPopup(eHtml); 
    }); 
}


