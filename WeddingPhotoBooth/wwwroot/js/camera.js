const videoElement = document.querySelector('video');
const videoSelect = document.querySelector('select#videoSource');

function changeCamera(stream) {
    document.querySelector('video').src = stream.url;
}

function gotDevices(deviceInfos) {
    for (let i = 0; i !== deviceInfos.length; ++i) {
        const deviceInfo = deviceInfos[i];
        const option = document.createElement('option');
        option.value = deviceInfo.deviceId;

        if (deviceInfo.kind === 'videoinput') {
            option.text = deviceInfo.label || 'camera ' +
                (videoSelect.length + 1);
            videoSelect.appendChild(option);
        } else {
            console.log('Found another kind of device: ', deviceInfo);
        }
    }
    const source = document.querySelector("#videoSource option");
    source.setAttribute("selected", "true");
    //$('#videoSource option:first').prop("selected", true);
}

function getStream() {
    if (window.stream) {
        window.stream.getTracks().forEach(function (track) {
            track.stop();
        });
    }

    const constraints = {
        video: {
            deviceId: { exact: videoSelect.value },
            width: 1680,
            height: 1050
        }
    };

    navigator.mediaDevices.getUserMedia(constraints)
        .then(stream => videoElement.srcObject = stream)
        .then(() => log(video.videoWidth + "x" + video.videoHeight))
        .catch(e => console.log(e));
}

function gotStream(stream) {
    window.stream = stream; // make stream available to console
    videoElement.srcObject = stream;
}

function handleError(error) {
    console.error('Error: ', error);
}