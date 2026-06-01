// Auto-dismiss alerts after 5s
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.alert.alert-success').forEach(function (el) {
        setTimeout(function () {
            var alert = bootstrap.Alert.getOrCreateInstance(el);
            if (alert) alert.close();
        }, 5000);
    });
});
