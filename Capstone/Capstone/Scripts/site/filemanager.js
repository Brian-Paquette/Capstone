// ---------------------------------------
// ABZ Capstone
// ---------------------------------------


// Manages upload button name change & generation button availability
$(document).on('change', '#schedule-upload-fromFile :file', function () {
    var input = $(this), label = input.val().replace(/\\/g, '/').replace(/.*\//, '');
    if (input.val()) {
        $('#schedule-generate').css("display", "inline-block");
        $('#schedule-upload-fromFile .btn-label').text(label);
    }
});