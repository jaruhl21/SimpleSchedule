window.onload = function () {
    document.getElementById('location-section').style.display = 'none';
};

function handleClick(radioButton) {
    if (radioButton.value == "Business Trip") {
        document.getElementById('location-section').style.display = 'block';
    }
    else {
        document.getElementById('location-section').style.display = 'none';
    }
}