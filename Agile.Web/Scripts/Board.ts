/// <reference path="../../framework/signum.web/signum/scripts/globals.ts"/>

import Entities = require("Framework/Signum.Web/Signum/Scripts/Entities")
import Navigator = require("Framework/Signum.Web/Signum/Scripts/Navigator")
import Finder = require("Framework/Signum.Web/Signum/Scripts/Finder")
import Lines = require("Framework/Signum.Web/Signum/Scripts/Lines")
import Validator = require("Framework/Signum.Web/Signum/Scripts/Validator")
import Operations = require("Framework/Signum.Web/Signum/Scripts/Operations")

export function init() {
    var board = $("div.board");

    board.closest("container").remove("container").addClass("container-fluid");
}


export function initList(urlCreate: string) {

    var board = $("div.board");

    function editMode(addCard : JQuery) {
        addCard.hide();

        var block = addCard.siblings(".list-create-card-block");
        block.show();
        block.children("textarea").focus();
    }

    board.on("click", "a.list-add-card", e=> {
        editMode($(e.currentTarget));
    }); 

    board.on("click", "a.list-create-card-cancel", e=> {
        var block = $(e.currentTarget).closest(".list-create-card-block");
        block.hide();
        block.siblings(".list-add-card").show();
    });

    function createCard(textArea : JQuery) {
        var list = textArea.closest(".list").attr("data-list");

        var text = textArea.val();

        var board = $("div.board").attr("data-board");

        SF.ajaxPost({
            url: urlCreate,
            data: { list: list, text: text, board: board }
        }).then(bHtml=> {
            textArea.closest(".board").replaceWith($(bHtml));

            editMode($(".list[data-list='" + list + "'] a.list-add-card"));
        }); 
    }

    board.on("keydown", "textarea", e=> {
        if (e.keyCode == 13)
            createCard($(e.currentTarget));
    });

    board.on("click", ".list-create-card-button", e=> {
        var textArea = $(e.currentTarget).closest(".list-create-card-block").children("textarea");
        createCard(textArea);
    });
}

export function initCard(saveUrl : string) {
    var board = $("div.board");

    board.on("click", "div.card", e=> {

        var liteKey = $(e.currentTarget).attr("data-card");

        var eHtml = new Entities.EntityHtml("Card", Entities.RuntimeInfo.fromKey(liteKey));

        Navigator.viewPopup(eHtml, { saveProtected: false }).then(html=> {
            if (html) {
                var formData = Validator.getFormValuesHtml(html, "prefix");

                formData["board"] = board.attr("data-board");

                return SF.ajaxPost({ url: saveUrl, data: formData })
                    .then(bHtml=> {
                        board.replaceWith($(bHtml));
                });
            }
        });; 
    }); 
}


export function initDrag(urlMove: string) {
    var board = $("div.board");

    board.on("dragstart", ".card", e => {
        var de = <DragEvent><Event>e.originalEvent;

        var currentCard = $(e.currentTarget);
        de.dataTransfer.effectAllowed = "move";
        de.dataTransfer.setData("Text", currentCard.attr("data-card"));

        currentCard.prev().addClass("avoid-dragging");
        currentCard.next().addClass("avoid-dragging");

        board.addClass("board-dragging");
    }); 

    board.on("dragend", ".card", e => {
        board.removeClass("board-dragging");
        board.find(".avoid-dragging").removeClass("avoid-dragging");
    });

    board.on("dragover", ".card-separator:not(.avoid-dragging)", e=> {
        var de = <DragEvent><Event>e.originalEvent;
        de.preventDefault();

        var sep = $(e.currentTarget)

        sep.addClass("card-over");
        de.dataTransfer.dropEffect = "move";
    });

    board.on("dragleave", ".card-separator:not(.avoid-dragging)", e=> {
        var de = <DragEvent><Event>e.originalEvent;
        de.preventDefault();
        de.dataTransfer.dropEffect = "move";

        $(e.currentTarget).removeClass("card-over");
    });

    board.on("drop", ".card-separator:not(.avoid-dragging)", e=> {
        var de = <DragEvent><Event>e.originalEvent;
        de.preventDefault();
        var sep = $(e.currentTarget);

        var list = sep.closest(".list").attr("data-list");
        var card = de.dataTransfer.getData("Text");
        var next = sep.next().attr("data-card");
        var prev = sep.prev().attr("data-card");

        SF.ajaxPost({
            url: urlMove,
            data: { list: list, card: card, next: next, prev: prev, board : board.attr("data-board") }
        }).then(bHtml=> {
            sep.closest(".board").replaceWith($(bHtml));
        });
    }); 
}


