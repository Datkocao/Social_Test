namespace Social_Test.Models
{
    public record Preset(
        string Group,
        string Name,
        string Method,
        string Path,
        string Kind,            // none | json | form
        string? Body = null,
        string? FileField = null);

    public class ResultVm
    {
        public string Request { get; set; } = "";
        public int Status { get; set; }
        public string Reason { get; set; } = "";
        public long Ms { get; set; }
        public string Body { get; set; } = "";
    }

    public class TesterVm
    {
        public string Slot { get; set; } = "A";
        public string Method { get; set; } = "GET";
        public string Path { get; set; } = "/api/ai/health";
        public string Kind { get; set; } = "none";
        public string? Body { get; set; }
        public string FileField { get; set; } = "File";
        public bool NoAuth { get; set; }

        public ResultVm? Result { get; set; }
        public string? Notice { get; set; }

        public string ApiBase { get; set; } = "";
        public string GoogleClientId { get; set; } = "";
        public Dictionary<string, string?> Who { get; set; } = new();   // slot -> email (null = chưa đăng nhập)
        public List<string> Ids { get; set; } = new();
    }

    public static class Presets
    {
        public static readonly List<Preset> All = new()
        {
            // ---------- Hệ thống ----------
            new("0. Hệ thống", "AI health (không cần token)", "GET", "/api/ai/health", "none"),
            new("0. Hệ thống", "TURN credentials", "GET", "/api/turn/credentials", "none"),

            // ---------- Auth ----------
            new("1. Auth", "Security info", "GET", "/api/auth/security-info", "none"),
            new("1. Auth", "Forgot password", "POST", "/api/auth/forgot-password", "json", """{"email":"a@test.com"}"""),
            new("1. Auth", "Verify OTP", "POST", "/api/auth/verify-otp", "json", """{"email":"a@test.com","token":"123456"}"""),
            new("1. Auth", "Reset password", "POST", "/api/auth/reset-password", "json",
                """{"email":"a@test.com","verifyToken":"<token từ verify-otp>","newPassword":"Abc@12345","confirmNewPassword":"Abc@12345"}"""),
            new("1. Auth", "Send set-password OTP (tài khoản Google)", "POST", "/api/auth/send-set-password-otp", "none"),
            new("1. Auth", "Change password", "PUT", "/api/auth/change-password", "json",
                """{"oldPassword":"Abc@12345","newPassword":"Abc@123456","confirmNewPassword":"Abc@123456"}"""),
            new("1. Auth", "Revoke refresh token", "POST", "/api/auth/revoke", "json", """{"refreshToken":"<refreshToken>"}"""),

            // ---------- Users ----------
            new("2. Users", "Hồ sơ của tôi", "GET", "/api/users/me", "none"),
            new("2. Users", "Hồ sơ người khác", "GET", "/api/users/{userId}", "none"),
            new("2. Users", "Cập nhật hồ sơ", "PUT", "/api/users/me", "json", """{"fullName":"Nguyễn Văn A","bio":"Xin chào"}"""),
            new("2. Users", "Đổi avatar (chọn file)", "PUT", "/api/users/me/avatar", "form", null, "File"),
            new("2. Users", "Đổi ảnh bìa (chọn file)", "PUT", "/api/users/me/cover", "form", null, "File"),
            new("2. Users", "Tìm user", "GET", "/api/users/search?q=a&page=1&size=10", "none"),
            new("2. Users", "Đổi nhạc chuông (chọn file)", "PUT", "/api/users/me/ringtone", "form", null, "File"),
            new("2. Users", "Xóa nhạc chuông", "DELETE", "/api/users/me/ringtone", "none"),
            new("2. Users", "Xem privacy settings", "GET", "/api/users/me/privacy-settings", "none"),
            new("2. Users", "Sửa privacy settings (0=Public,1=Friends,2=OnlyMe)", "PUT", "/api/users/me/privacy-settings", "json",
                """{"profileVisibility":0,"postVisibility":1,"friendListVisible":1,"searchDiscoverable":0}"""),

            // ---------- Friends ----------
            new("3. Friends", "Gửi lời mời kết bạn", "POST", "/api/friends/request", "json", """{"receiverId":"{userId của B}"}"""),
            new("3. Friends", "Lời mời đang chờ (nhận)", "GET", "/api/friends/requests/pending?page=1&size=10", "none"),
            new("3. Friends", "Lời mời đã gửi", "GET", "/api/friends/requests/sent?page=1&size=10", "none"),
            new("3. Friends", "Chấp nhận lời mời", "PUT", "/api/friends/request/{requestId}/accept", "none"),
            new("3. Friends", "Từ chối lời mời", "PUT", "/api/friends/request/{requestId}/reject", "none"),
            new("3. Friends", "Danh sách bạn bè", "GET", "/api/friends?page=1&size=10", "none"),
            new("3. Friends", "Gợi ý kết bạn", "GET", "/api/friends/suggestions?page=1&size=10", "none"),
            new("3. Friends", "Trạng thái bạn bè với user", "GET", "/api/friends/status/{userId}", "none"),
            new("3. Friends", "Hủy kết bạn", "DELETE", "/api/friends/{userId}", "none"),
            new("3. Friends", "Chặn user", "POST", "/api/friends/block/{userId}", "none"),
            new("3. Friends", "Bỏ chặn user", "DELETE", "/api/friends/block/{userId}", "none"),

            // ---------- Posts ----------
            new("4. Posts", "Tạo bài (form; Privacy 0/1/2; chọn file tùy ý)", "POST", "/api/posts", "form",
                "Content=Bài viết test\nPrivacy=0", "MediaFiles"),
            new("4. Posts", "Xem 1 bài", "GET", "/api/posts/{postId}", "none"),
            new("4. Posts", "Sửa bài", "PUT", "/api/posts/{postId}", "json", """{"content":"Nội dung đã sửa","privacy":0}"""),
            new("4. Posts", "Xóa bài", "DELETE", "/api/posts/{postId}", "none"),
            new("4. Posts", "Feed", "GET", "/api/posts/feed?page=1&size=10", "none"),
            new("4. Posts", "Bài của 1 user", "GET", "/api/users/{userId}/posts?page=1&size=10", "none"),
            new("4. Posts", "Tìm bài", "GET", "/api/posts/search?q=test&type=all&page=1&size=10", "none"),
            new("4. Posts", "Like / Unlike (toggle)", "POST", "/api/posts/{postId}/like", "none"),
            new("4. Posts", "Danh sách comment", "GET", "/api/posts/{postId}/comments?page=1&size=10", "none"),
            new("4. Posts", "Thêm comment", "POST", "/api/posts/{postId}/comments", "json", """{"content":"Comment test"}"""),
            new("4. Posts", "Trả lời comment", "POST", "/api/posts/{postId}/comments", "json", """{"content":"Reply test","parentCommentId":"{commentId}"}"""),
            new("4. Posts", "Xóa comment", "DELETE", "/api/comments/{commentId}", "none"),
            new("4. Posts", "Chia sẻ lên feed", "POST", "/api/posts/{postId}/share-to-feed", "json", """{"content":"Chia sẻ lại","privacy":0}"""),
            new("4. Posts", "Lấy link share", "GET", "/api/posts/{postId}/share", "none"),

            // ---------- Notifications ----------
            new("5. Notifications", "Danh sách", "GET", "/api/notifications?page=1&size=10", "none"),
            new("5. Notifications", "Số chưa đọc", "GET", "/api/notifications/count", "none"),
            new("5. Notifications", "Đánh dấu đã đọc", "PUT", "/api/notifications/read", "json", """{"notificationIds":["{notificationId}"]}"""),
            new("5. Notifications", "Đọc tất cả", "PUT", "/api/notifications/read-all", "none"),
            new("5. Notifications", "Xóa 1 thông báo", "DELETE", "/api/notifications/{notificationId}", "none"),

            // ---------- Messages ----------
            new("6. Messages", "Tạo/lấy hội thoại 1-1", "POST", "/api/conversations", "json", """{"participantIds":["{userId của B}"],"isGroup":false}"""),
            new("6. Messages", "Tạo nhóm chat", "POST", "/api/conversations", "json", """{"participantIds":["{userId}","{userId}"],"isGroup":true,"groupName":"Nhóm test"}"""),
            new("6. Messages", "Danh sách hội thoại", "GET", "/api/conversations?page=1&size=20", "none"),
            new("6. Messages", "Tin nhắn trong hội thoại", "GET", "/api/conversations/{conversationId}/messages?page=1&size=20", "none"),
            new("6. Messages", "Gửi tin nhắn (form; file đính kèm tùy ý)", "POST", "/api/conversations/{conversationId}/messages", "form",
                "ConversationId={conversationId}\nContent=Xin chào", "Attachment"),
            new("6. Messages", "Đánh dấu đã xem", "PUT", "/api/conversations/{conversationId}/seen", "none"),
            new("6. Messages", "Xóa tin nhắn", "DELETE", "/api/messages/{messageId}", "none"),
            new("6. Messages", "Emojis", "GET", "/api/emojis", "none"),
            new("6. Messages", "Emojis theo nhóm", "GET", "/api/emojis/smileys", "none"),
            new("6. Messages", "Tìm GIF", "GET", "/api/gifs/search?q=cat&limit=10", "none"),
            new("6. Messages", "GIF trending", "GET", "/api/gifs/trending?limit=10", "none"),

            // ---------- Groups ----------
            new("7. Groups", "Tạo nhóm (form; Privacy 0=Public,1=Private)", "POST", "/api/groups", "form",
                "Name=Nhóm test\nDescription=Mô tả\nPrivacy=0\nRequireApproval=false\nRequirePostApproval=false", "Avatar"),
            new("7. Groups", "Chi tiết nhóm", "GET", "/api/groups/{groupId}", "none"),
            new("7. Groups", "Tìm nhóm", "GET", "/api/groups/search?keyword=test&page=1&size=20", "none"),
            new("7. Groups", "Nhóm của tôi", "GET", "/api/groups/mine?page=1&size=20", "none"),
            new("7. Groups", "Sửa nhóm (form)", "PUT", "/api/groups/{groupId}", "form", "Name=Tên mới\nDescription=Mô tả mới", "Avatar"),
            new("7. Groups", "Xóa nhóm", "DELETE", "/api/groups/{groupId}", "none"),
            new("7. Groups", "Tham gia nhóm", "POST", "/api/groups/{groupId}/join", "none"),
            new("7. Groups", "Rời nhóm", "POST", "/api/groups/{groupId}/leave", "none"),
            new("7. Groups", "Hủy yêu cầu tham gia", "DELETE", "/api/groups/{groupId}/join-request", "none"),
            new("7. Groups", "Thành viên", "GET", "/api/groups/{groupId}/members?page=1&size=20", "none"),
            new("7. Groups", "Kick thành viên", "DELETE", "/api/groups/{groupId}/members/{userId}", "none"),
            new("7. Groups", "Đổi vai trò (0=Member,1=Admin)", "PUT", "/api/groups/{groupId}/members/{userId}/role", "json", """{"role":1}"""),
            new("7. Groups", "Chuyển quyền chủ nhóm", "POST", "/api/groups/{groupId}/transfer-ownership", "json", """{"newOwnerId":"{userId}"}"""),
            new("7. Groups", "Yêu cầu tham gia đang chờ", "GET", "/api/groups/{groupId}/join-requests?page=1&size=20", "none"),
            new("7. Groups", "Duyệt yêu cầu tham gia", "PUT", "/api/groups/{groupId}/join-requests/{requestId}", "json", """{"approve":true}"""),
            new("7. Groups", "Feed nhóm", "GET", "/api/groups/{groupId}/posts?page=1&size=10", "none"),
            new("7. Groups", "Đăng bài trong nhóm (form)", "POST", "/api/groups/{groupId}/posts", "form", "Content=Bài trong nhóm\nPrivacy=0", "MediaFiles"),
            new("7. Groups", "Bài chờ duyệt", "GET", "/api/groups/{groupId}/posts/pending?page=1&size=20", "none"),
            new("7. Groups", "Duyệt bài nhóm", "PUT", "/api/groups/{groupId}/posts/{postId}/review", "json", """{"approve":true}"""),

            // ---------- AI ----------
            new("8. AI", "Chat với AI", "POST", "/api/ai/chat", "json", """{"conversationId":"{conversationId}","history":[],"newMessage":"Xin chào"}"""),

            // ---------- Admin (đăng nhập tài khoản Admin) ----------
            new("9. Admin", "Dashboard", "GET", "/api/admin/dashboard", "none"),
            new("9. Admin", "Danh sách bài", "GET", "/api/admin/posts?page=1&size=10", "none"),
            new("9. Admin", "Xóa bài", "DELETE", "/api/admin/posts/{postId}", "none"),
            new("9. Admin", "Khôi phục bài", "PUT", "/api/admin/posts/{postId}/restore", "none"),
            new("9. Admin", "Danh sách user", "GET", "/api/admin/users?page=1&size=10", "none"),
            new("9. Admin", "Ban user", "PUT", "/api/admin/users/{userId}/ban", "json", """{"reason":"Vi phạm"}"""),
            new("9. Admin", "Unban user", "PUT", "/api/admin/users/{userId}/unban", "none"),
            new("9. Admin", "Cloud stats", "GET", "/api/admin/cloud/stats", "none"),
        };
    }
}
