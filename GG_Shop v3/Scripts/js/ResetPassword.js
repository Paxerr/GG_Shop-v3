$(function () {

    // Toggle password visibility
    $('.password-toggle').on('click', function () {
        const $input = $('#NewPassword');
        const isPassword = $input.attr('type') === 'password';
        $input.attr('type', isPassword ? 'text' : 'password');
        $(this).toggleClass('visible');
    });

    const $form = $('#resetForm');
    const $btn = $('#resetSubmit');
    const $spinner = $btn.find('.btn-spinner');

    $form.on('submit', function (e) {
        e.preventDefault();

        $('#passError').text('');

        $btn.prop('disabled', true);
        $spinner.removeClass('hidden');

        $.ajax({
            url: $form.attr('action'),
            method: 'POST',
            data: $form.serialize(),
            success: function (res) {
                if (res.success) {
                    alert("Đặt lại mật khẩu thành công!");
                    window.location.href = res.redirectUrl;
                } else if (res.errors) {
                    if (res.errors.NewPassword) {
                        $('#passError').text(res.errors.NewPassword.join(', '));
                    }
                    if (res.errors._global) {
                        alert(res.errors._global.join('\n'));
                    }
                } else {
                    alert("Có lỗi xảy ra, vui lòng thử lại.");
                }
            },
            error: function () {
                alert("Không thể xử lý yêu cầu, vui lòng thử lại sau.");
            },
            complete: function () {
                $btn.prop('disabled', false);
                $spinner.addClass('hidden');
            }
        });
    });
});
