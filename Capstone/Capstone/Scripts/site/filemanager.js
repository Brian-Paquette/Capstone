
// Manages upload button name change & generation button availability
$(document).on('change', '#schedule-upload :file', function () {
    var input = $(this), label = input.val().replace(/\\/g, '/').replace(/.*\//, '');
    if (input.val()) {
        $('#schedule-generate').css("display", "block");
        $('#schedule-upload .btn-label').text(label);
    }
});