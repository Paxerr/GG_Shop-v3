$(function () {
    const $form = $('#forgotForm');
    const $btn = $('#forgotSubmit');
    const $spinner = $btn.find('.btn-spinner');

    $form.on('submit', function (e) {
        e.preventDefault();

        $('#emailError').text('');

        $btn.prop('disabled', true);
        $spinner.removeClass('hidden');

        $.ajax({
            url: $form.attr('action'),
            method: 'POST',
            data: $form.serialize(),
            success: function (res) {
                if (res.success) {
                    alert("Mã đặt lại mật khẩu đã được gửi đến email của bạn!");
                    window.location.href = res.redirectUrl;
                } else if (res.errors) {
                    if (res.errors.Email) {
                        $('#emailError').text(res.errors.Email.join(', '));
                    }
                    if (res.errors._global) {
                        alert(res.errors._global.join('\n'));
                    }
                } else {
                    alert("Có lỗi xảy ra, vui lòng thử lại!");
                }
            },
            error: function () {
                alert("Không thể gửi yêu cầu. Vui lòng thử lại sau!");
            },
            complete: function () {
                $btn.prop('disabled', false);
                $spinner.addClass('hidden');
            }
        });
    });
});
