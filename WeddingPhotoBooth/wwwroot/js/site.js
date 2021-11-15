const img = $('#screenshot img')[0];
const canvas = document.createElement('canvas');
const prevcanvas = document.createElement('canvas');

function init() {
    $('#start-action-container').modal('show');
    initSpacebarListener();
    initStartButtonClickListener();

    navigator.mediaDevices.enumerateDevices()
        .then(gotDevices).then(getStream).catch(handleError);

    videoSelect.onchange = getStream;
}

function generateSessionKeyAndStart() {
    $(window).off('keypress');
    $('#btn-start').off('click');
    $.ajax({
        type: 'POST',
        url: "?handler=GenerateSessionKey",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        success: function (response) {
            start();
        },
        fail: function (response) {
            initSpacebarListener();
            initStartButtonClickListener();
            alert('An error occured... :(');
        }
    });
}

function initSpacebarListener() {
    $(window).on('keypress', function (event) {
        if (event.which === 32) {
            $('#btn-start').focus();
            generateSessionKeyAndStart();
        }
    });
}

function initStartButtonClickListener() {
    $('#btn-start').on('click touchstart', function (event) {
        if (event.type === "click") {
            if (event.which === 1) {
                generateSessionKeyAndStart();
            }
        }
        else if (event.type === "touchend") {
            generateSessionKeyAndStart();
        }
    });
}

function logAction(action) {
    $.ajax({
        type: 'POST',
        url: "?handler=LogAction",
        data: JSON.stringify(action),
        contentType: 'application/json',
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        success: function (response) {
            console.log(action);
        }
    });
}

function deleteSession() {
    $.ajax({
        type: 'POST',
        url: "?handler=DeleteSession",
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        success: function (response) {
            window.location.reload();
        },
        fail: function (response) {
            alert('An error occurred... :(');
        }
    });
}

function start() {
    setTimeout(function () {
        $('#start-action-container').modal('hide');
        $('#take-photo-action-container').modal('show');
        $('#btn-start-over').removeClass('hidden');
        $('#prev-img-container').removeClass('hidden');
        $('#countdown-container')
            .removeClass('hidden')
            .append('<h1></h1>');
        setTimeout(function () {
            $('#countdown-container > h1').addClass('slide-down');
            $('#btn-start-over').addClass('slide-down');
            $('#prev-img-container').addClass('slide-in');
            takePhotoCountdown(1);
        }, 500);
    }, 500);
}

function takePhotoCountdown(photoNumber) {
    var numberText;
    if (photoNumber === 1) {
        numberText = 'first';
    }
    else if (photoNumber === 2) {
        numberText = 'second';
    }
    else if (photoNumber === 3) {
        numberText = 'third';
    }
    else {
        return;
    }
    setTimeout(function () {
        $('#countdown-container > h1')
            .removeClass('slide-down');
        setTimeout(function () {
            $('#countdown-container > h1')
                .text('Get ready for the  ' + numberText + ' photo...')
                .addClass('slide-down');
            setTimeout(function () {
                $('#countdown-container > h1')
                    .removeClass('slide-down');
                setTimeout(function () {
                    $('#countdown-container > h1')
                        .text('3')
                        .addClass('countdown-number-text')
                        .addClass('slide-number-down');
                    setTimeout(function () {
                        $('#countdown-container > h1')
                            .text('2');
                        setTimeout(function () {
                            $('#countdown-container > h1')
                                .text('1');
                            setTimeout(function () {
                                $('#prev-img-container').removeClass('slide-in');
                            }, 1000);
                            setTimeout(function () {
                                $('#flash-container').removeClass('hidden');
                            }, 1500);
                            setTimeout(function () {
                                $('#countdown-container > h1')
                                    .removeClass('countdown-number-text')
                                    .removeClass('slide-number-down');
                                takePhoto(photoNumber);
                            }, 2000);
                        }, 1500);
                    }, 1500);
                }, 500);
            }, 4000);
        }, 500);
    }, 500);
}



function takePhoto(photoNumber) {
    let vw = videoElement.videoWidth;
    let vh = videoElement.videoHeight;
    canvas.width = vw;
    canvas.height = vh;
    prevcanvas.width = vw / 6.0
    prevcanvas.height = vh / 6.0;

    let context = canvas.getContext('2d');
    context.scale(-1, 1);
    let prevcontext = prevcanvas.getContext('2d');
    prevcontext.scale(-1, 1);

    context.drawImage(videoElement, 0, 0, canvas.width * -1, canvas.height);
    prevcontext.drawImage(videoElement, 0, 0, prevcanvas.width * -1, prevcanvas.height);

    var image = canvas.toDataURL("image/png");
    var previmage = prevcanvas.toDataURL("image/png");
    img.src = image;

    $('#screenshot').removeClass('hidden');
    $('#flash-container').addClass('hidden');
    logAction(`Photo ${photoNumber} taken`);
    if (photoNumber === 1) {
        $('#p1')[0].src = image;
        $('#pprev1')[0].src = previmage;
        setTimeout(function () {
            $('#screenshot').addClass('hidden');
            $('#prev-img-container').addClass('slide-in');
            takePhotoCountdown(photoNumber += 1);
        }, 1500);
    }
    else if (photoNumber === 2) {
        $('#p2')[0].src = image;
        $('#pprev2')[0].src = previmage;
        setTimeout(function () {
            $('#screenshot').addClass('hidden');
            $('#prev-img-container').addClass('slide-in');
            $('#prev-img-container').removeClass('hidden');
            takePhotoCountdown(photoNumber += 1);
        }, 1500);
    }
    else if (photoNumber === 3) {
        $('#p3')[0].src = image;
        $('#pprev3')[0].src = previmage;
        submitPhotos();
    }
}

function submitPhotos() {
    var p1 = $('#p1')[0].src.replace('data:image/png;base64,', '');
    var p2 = $('#p2')[0].src.replace('data:image/png;base64,', '');
    var p3 = $('#p3')[0].src.replace('data:image/png;base64,', '');

    $.ajax({
        type: 'POST',
        url: "?handler=SubmitPhotos",
        data: JSON.stringify([p1, p2, p3]),
        contentType: 'application/json',
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        success: function (response) {
            setTimeout(function () {
                $('#screenshot').addClass('hidden');
                $('#prev-img-container').addClass('hidden');
                $('#exampleModalCenter').modal('show');
            }, 2000);

        },
        fail: function (response) {
            alert('An error occured! Retake photo...');
        }
    });
}

function emailOptionSelected() {
    $('#email-input').prop('required', true);
}

function setEmailButtonDetails(option) {
    if (option === 1) {
        $('#email-input').val('');
        $('#email-collapse-complete').attr('onclick', 'complete(1)').text('📧 Send');
        setTimeout(function () {
            $('#email-input').focus();
        }, 250);
    }
    else if (option === 3) {
        $('#email-input').val('');
        $('#email-collapse-complete').attr('onclick', 'complete(3)').text('📧 Send & 🖨 Print');
        setTimeout(function () {
            $('#email-input').focus();
        }, 250);
    }
    $('.collapseOptions').collapse('toggle');
    $('.collapseEmail').collapse('toggle');
}

function initComplete(option) {
    if (option === 1) {
        $('#collapseSendingMessage').empty();
        $('#collapseSendingMessage').append('<h1 class="saving">📧 Your photos will be emailed!<span>.</span><span>.</span><span>.</span></h1>');
        $('.collapseOptions').collapse('hide');
        $('.collapseEmail').collapse('hide');
        $('.collapseSending').collapse('show');
    }
    if (option === 2) {
        $('#collapseSendingMessage').empty();
        $('#collapseSendingMessage').append('<h1>🖨 Your photo is printing, go check the printer!</h1>');
        $('.collapseOptions').collapse('hide');
        $('.collapseEmail').collapse('hide');
        $('.collapseSending').collapse('show');
    }
    if (option === 3) {
        $('#collapseSendingMessage').empty();
        $('#collapseSendingMessage').append('<h1 class="saving">🖨 Printing & 📧 Your photos will be emailed<span>.</span><span>.</span><span>.</span></h1>');
        $('.collapseOptions').collapse('hide');
        $('.collapseEmail').collapse('hide');
        $('.collapseSending').collapse('show');
    }
    $('#btn-start-over').removeClass('slide-down');
}

function complete(option) {
    initComplete(option);
    $.ajax({
        type: 'POST',
        url: "?handler=Complete",
        data: JSON.stringify({ "option": option, "emailAddress": $('#email-input').val() }),
        contentType: 'application/json',
        beforeSend: function (xhr) {
            xhr.setRequestHeader("XSRF-TOKEN",
                $('input:hidden[name="__RequestVerificationToken"]').val());
        },
        success: function (response) {
            if (option === 1) {
                $('#collapseSendingMessage').empty();
                $('#collapseSendingMessage').append('<h1 class="text-success">📧 Your photos will be emailed!</h1>');
            }
            else if (option === 3) {
                $('#collapseSendingMessage').empty();
                $('#collapseSendingMessage').append('<h1 class="text-success">📧 Your photos will be emailed!</h1>');
                $('#collapseSendingMessage').append('<h1>🖨 Your photo is printing, go check the printer!</h1>');
            }
            setTimeout(function () {
                window.location.reload();
            }, 5000);
        },
        error: function (response) {
            $('#btn-start-over').addClass('slide-down');
            if (option === 2) {
                $('.collapseOptions').collapse('show');
            }
            else {
                $('.collapseEmail').collapse('show');
            }
            $('.collapseSending').collapse('hide');
            alert('An error occurred... :(');
        }
    });
}

function backToOptions() {
    $('.collapseEmail').collapse('hide');
    $('.collapseOptions').collapse('show');
}

$(document).ready(function () {
    $('#completeForm').on('submit', function (e) {
        e.preventDefault();
    });
    init();
});

window.addEventListener("contextmenu", function (e) {
    e.preventDefault();
})