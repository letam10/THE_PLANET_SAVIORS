# THE_PLANET_SAVIORS — Codex workflow

## 0. LƯU Ý CỐT LÕI
- **Ngôn ngữ:** Luôn trả lời và giao tiếp bằng tiếng Việt.
- **Công cụ:** Luôn sử dụng MCP tools để chỉnh sửa scene, prefab, component thay vì sửa file text thủ công.
- **An toàn:** Luôn kiểm tra lỗi biên dịch trước khi thực hiện commit.

## 0.1 Browser Input Workflow (Quy trình tương tác Web)
Áp dụng nghiêm ngặt cho mọi ô nhập dữ liệu trên trình duyệt:
1. **Click/Focus** vào ô nhập liệu.
2. **Đợi 2-3 giây** để trình duyệt ổn định.
3. **Nhập nội dung**.
4. **Đợi 1 giây**.
5. **Kiểm tra hiển thị** bằng cách chụp ảnh màn hình (Snapshot).
6. **Nhấn gửi**.

## 1. Mandatory Workflow & Planning (Quy trình Bắt buộc)
- Đối với mọi tác vụ phức tạp, **phải lập kế hoạch dưới dạng bảng Markdown** (theo format ở mục 5).
- Sau khi xuất bảng kế hoạch, **dừng lại và chờ người dùng phê duyệt** (explicit user approval) mới được làm tiếp.
- **Quy tắc Nhánh (Branch):** Luôn làm việc trên branch hiện tại. **Tuyệt đối KHÔNG thao tác trực tiếp trên `main`**. Ưu tiên đặt tên nhánh theo cú pháp: `codex/<task-name>`.
- **Chia nhỏ công việc (Checkpoints):** 1. Review git diff hiện tại.
  2. Stage các file liên quan. **QUAN TRỌNG: Luôn stage file `.meta` đi kèm với file assets/scripts bị thay đổi.**
  3. Tạo một commit cho checkpoint đó.
  4. Chuyển sang checkpoint tiếp theo.

## 2. Unity & Architecture Execution Rules (Quy tắc Dự án TPS)
- **Cấu trúc thư mục:** Mã nguồn (code) lưu nghiêm ngặt tại `Assets/_TPS/Scripts/...` và dữ liệu (content) tại `Assets/_TPS/Data/...`. **Tuyệt đối không chỉnh sửa** `Library`, `Temp`, `Logs`, hoặc `PackageCache`.
- **Namespaces:** Bắt buộc dùng chuẩn `TPS.[Module]` (vd: `TPS.Runtime.Combat`).
- **Data-Driven (Dữ liệu điều khiển):**
  - **ScriptableObjects (SOs)** chỉ là bản mẫu (Read-only khi chạy). **KHÔNG BAO GIỜ** ghi trạng thái runtime vào SOs.
  - **Runtime State** phải lưu trong các lớp POCO (vd: `SaveData`) và quản lý qua `GameStateManager`.
- **Events:** Dùng `GameEventBus` CHỈ cho các sự kiện vĩ mô (macro events). Dùng tham chiếu trực tiếp (`[SerializeField]`) hoặc C# actions cho logic nội bộ.

## 3. Commit Policy (Chính sách Commit)
- **Không dồn code:** Tránh tạo một commit khổng lồ vào cuối ngày. Ưu tiên các commit nhỏ, rõ ràng.
- **Tiền tố chuẩn:** Sử dụng `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`, `ui:`.
- Nếu diff quá lớn, hãy tách ra trước khi commit.
- **BẢO VỆ DỮ LIỆU:** **KHÔNG BAO GIỜ** commit scenes (`.unity`) hoặc prefabs (`.prefab`) nếu tác vụ chỉ yêu cầu sửa code C#, nhằm tránh xung đột tuần tự hóa (serialization conflicts).
- **Không tự ý tạo PR** hoặc merge vào `main` trừ khi được yêu cầu.

## 4. Validation Before Commit (Kiểm tra trước khi Commit)
- Nếu có thay đổi code, chạy các bước kiểm tra (validation) nhỏ gọn nhất có thể trước mỗi checkpoint. Ưu tiên kiểm tra hẹp thay vì rebuild toàn bộ dự án.
- **Kiểm tra đặc thù Unity:**
  - Đảm bảo không có lỗi biên dịch (compiler errors/warnings) mới trong Unity Console.
  - Đảm bảo không có tham chiếu bị gãy (`Missing (Mono Script)` hoặc `Null Reference`) trong các scene/prefab vừa đụng tới.
- Nếu kiểm tra thất bại: **Phải sửa lỗi trước** hoặc dừng lại và báo cáo rõ ràng cho người dùng, KHÔNG được commit mã lỗi.

## 5. Planning Table Format (Định dạng Bảng Kế Hoạch)
| Section | Content |
|---|---|
| Goal | Mục tiêu của tác vụ |
| Files & Assets to change | Bao gồm các file .cs, .prefab, .asset, và .meta sẽ bị ảnh hưởng |
| Risks | Các rủi ro (vd: Lỗi Save/Load, Override Prefab, Xung đột Event) |
| Validation | Các bước kiểm tra (vd: "Biên dịch không lỗi, kiểm tra tham chiếu Inspector") |
| Commit message | Nội dung commit dự kiến |