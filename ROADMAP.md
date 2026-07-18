# Edge Workspace Manager Roadmap

## Next Update Queue

ยังไม่ได้กำหนดหัวข้อและหมายเลขเวอร์ชันสำหรับอัปเดตถัดไป

## Completed in v2.1.2

### Single Instance Protection

- อนุญาตให้เปิด Edge Workspace Manager ได้เพียงหนึ่ง Process ต่อ Windows session
- เมื่อเปิดซ้ำ ให้แสดงข้อความว่าโปรแกรมกำลังเปิดใช้งานอยู่แล้วและปิดเฉพาะ Process ใหม่
- ห้ามกระทบ Tab, Login, Session หรือหน้าต่างเดิมที่กำลังใช้งาน
- หาก Process เดิม Crash หรือปิดสมบูรณ์ ต้องสามารถเปิดโปรแกรมใหม่ได้ตามปกติ
- Updater ต้องเปิดเวอร์ชันใหม่ได้หลัง Process เดิมปิด โดยไม่ติด Single Instance lock ค้าง

### Single Instance Acceptance Criteria

- เปิดโปรแกรมครั้งแรกได้ตามปกติ
- เปิดโปรแกรมครั้งที่สองแล้วต้องเห็นข้อความแจ้งเตือนและไม่มี Main window ซ้ำ
- ปิดโปรแกรมเดิมแล้วต้องเปิดโปรแกรมใหม่ได้ทันที
- หลัง Update หรือ Rollback โปรแกรมต้องเปิดกลับได้เพียงหนึ่งหน้าต่าง

### Automatic Instance Tab Width

- กำหนดความกว้างของ Instance Tab ด้านบนตามความยาวชื่อ Instance โดยอัตโนมัติ
- เพิ่มระยะ Padding รอบข้อความเพื่อไม่ให้ชื่อถูกตัดชิดขอบ
- แสดงชื่อเต็มผ่าน Tooltip หากชื่อยาวเกินพื้นที่หน้าต่าง
- คำนวณขนาดใหม่ทันทีเมื่อเปลี่ยนชื่อ Instance และยังรองรับ DPI Scaling
- การปรับความกว้างต้องไม่กระทบสี Instance, Focus, การเลือก หรือการลากเรียงลำดับ

## Deferred

> รายการต่อไปนี้เลื่อนออกไปก่อน โดยยังไม่กำหนดหมายเลขเวอร์ชันหรือกำหนดการใหม่

### Import Preview, Merge Conflict and Automatic Rollback

- แสดง Preview รายการ Instance, Tab และ Favorites ก่อน Import
- ตรวจชื่อ Instance, Profile Folder, Tab และ Favorites ที่ซ้ำกับข้อมูลในเครื่อง
- เลือกวิธีจัดการ Conflict แบบ Import as new, Merge หรือ Replace รายการที่เลือก
- สำรอง Local Config อัตโนมัติก่อน Import
- Rollback ข้อมูลเดิมอัตโนมัติเมื่อ Import หรือการบันทึกล้มเหลว
- เพิ่ม Import History และรายละเอียดรายการที่เพิ่ม ข้าม รวม หรือย้อนกลับ

### Incognito Mode

- เพิ่มคำสั่ง `New Incognito Instance` และ `New Incognito Tab` พร้อมสัญลักษณ์ที่แยกจากโหมดปกติชัดเจน
- ใช้ WebView2 Profile ชั่วคราวแยกจาก Instance ปกติและไม่ใช้ Cookie, Login หรือ Session ร่วมกัน
- ไม่บันทึก History, Address Suggestions, Recently Closed Tabs, Favorites หรือข้อมูล Session Recovery
- ลบ Profile ชั่วคราวหลังปิด Incognito Instance หรือเมื่อปิดโปรแกรม โดย Retry การลบหาก WebView2 ยังถือไฟล์อยู่
- แสดงคำเตือนว่า Incognito ไม่สามารถซ่อนกิจกรรมจากเว็บไซต์ ผู้ให้บริการอินเทอร์เน็ต นายจ้าง หรือระบบเครือข่ายองค์กร
- Download ที่ผู้ใช้บันทึกลงดิสก์และ Bookmark ที่ผู้ใช้ส่งออกเองจะยังคงอยู่หลังปิด Incognito
- ไม่ดึง Password หรือข้อมูลจาก 1Password โดยอัตโนมัติ และไม่รวม Incognito ใน Metadata Backup หรือ Cross-device Sync

### Incognito Acceptance Criteria

- Incognito ต้องไม่เห็น Cookie หรือ Login จาก Profile ปกติ และ Profile ปกติต้องไม่เห็นข้อมูลจาก Incognito
- ปิด Incognito แล้ว History, URL, Tab และ Session ต้องไม่ปรากฏเมื่อเปิดโปรแกรมใหม่
- ปิด Tab หรือ Instance ปกติต้องไม่ลบ Profile หรือ Session ที่ใช้งานอยู่
- หากลบ Profile ชั่วคราวไม่สำเร็จ โปรแกรมต้องบันทึก Cleanup Queue และลองลบใหม่โดยไม่เปิดเผยข้อมูลใน UI
- การ Update, Crash Recovery และ Close Confirmation ต้องไม่เปลี่ยน Incognito ให้กลายเป็น Session ถาวร

### Live Theme Switching without Restart

Tracking issue: [#16 Dark Mode colors are inconsistent across toolbar, instances, and tabs](https://github.com/abnkei/edge-workspace-manager/issues/16)

- เปลี่ยน Theme ระหว่าง `Light`, `Dark` และ `Use Windows setting` ได้ทันทีหลังบันทึก Settings
- ใช้ Semantic Dark Palette เดียวกันกับ Toolbar, Container, Instance strip, Browser tab strip, Dialog, Grid และ Status bar
- ลด Saturation ของสี Instance ใน Dark Mode และใช้ Accent/Underline แสดง Focus แทนการเร่งความสว่างทั้ง Tab
- ทำปุ่ม `+` ให้กะทัดรัดและแยกจาก Content Tab อย่างชัดเจน
- รองรับ Dark Windows Title Bar ผ่าน DWM บน Windows ที่รองรับ
- อัปเดต Toolbar, Address Bar, Instance/Tab bar, Status bar, Menu, Dialog และหน้าต่าง Browser Tools ที่เปิดอยู่
- ไม่ Restart โปรแกรม ไม่สร้าง WebView ใหม่ และไม่ Reload หน้าเว็บ
- รักษา URL, Login, Cookie, Session, Focused Tab และตำแหน่ง Scroll ของหน้าเว็บเดิม
- คำนวณ Contrast ของ Focused Tab และ Instance Color ใหม่ตาม Theme ที่เลือก
- เมื่อเลือก `Use Windows setting` ให้เปลี่ยนตาม Windows Theme ระหว่างที่โปรแกรมเปิดอยู่
- แยก `Force dark web pages` ออกจาก Theme ของตัวโปรแกรมและแจ้งชัดเจนหากต้องเปิดโปรแกรมใหม่

### Live Theme Switching Acceptance Criteria

- เปลี่ยน Light/Dark/System แล้ว UI ทุกส่วนที่เปิดอยู่ต้องเปลี่ยนทันทีโดยไม่ Restart
- จำนวน WebView, Process, Instance และ Tab ต้องไม่เปลี่ยนจากการสลับ Theme
- Login, Session, URL ปัจจุบัน และสถานะ Pin ต้องคงเดิม
- ไม่มีพื้นหลังหรือข้อความสีเดิมค้างใน Menu, Dialog, Grid หรือ Tab ที่กำลัง Focus
- ไม่มีพื้นสีอ่อนค้างรอบ Instance strip หรือ Browser tab strip และสี Instance ต้องไม่ดูเรืองเกินไป
- Dark Mode ของโปรแกรมต้องไม่เปลี่ยนสีหน้าเว็บไซต์โดยอัตโนมัติ
- การสลับ Theme หลายครั้งติดต่อกันต้องไม่ทำให้ Event ซ้ำ Memory เพิ่มต่อเนื่อง หรือ UI ค้าง

### Password-encrypted Full Local Backup (Experimental)

- สำรอง Metadata พร้อม WebView2 Profile เป็นตัวเลือกขั้นสูง
- เข้ารหัส Backup ด้วยรหัสผ่านก่อนบันทึกลงดิสก์
- ปิด WebView2 อย่างปลอดภัยก่อนสำรองและตรวจว่าไม่มีไฟล์ Profile ถูกใช้งาน
- แสดงคำเตือนว่า Backup อาจมี Cookie, Login Token และข้อมูล Session ที่ละเอียดอ่อน
- ไม่รวมข้อมูลจาก 1Password และไม่รับประกันว่า Session ที่ผูกกับ Windows หรืออุปกรณ์จะย้ายข้ามเครื่องได้
- ทำเครื่องหมายเป็น Experimental จนผ่านการทดสอบ Recovery และ Compatibility

### Update Channels and Policy

- เพิ่ม Update Channel แบบ `Stable` และ `Beta`
- ดาวน์โหลดอัปเดตอัตโนมัติเบื้องหลังโดยไม่รบกวนการทำงาน
- เพิ่ม Critical Update Policy สำหรับอัปเดตความปลอดภัยหรือเวอร์ชันบังคับขั้นต่ำ
- แจ้งเตือน Critical Update ซ้ำตามนโยบายแม้ผู้ใช้เลือกเตือนภายหลัง
- รองรับ Minimum Supported Version และการกำหนดนโยบายจาก Update Manifest

## Completed in v2.1.1

### Close Confirmation

- แสดงหน้าต่างยืนยันเมื่อผู้ใช้กดปุ่ม `X`, `Alt+F4` หรือคำสั่งปิดโปรแกรม
- ใช้ข้อความ `ต้องการปิด Edge Workspace Manager หรือไม่?`
- ปุ่มเริ่มต้นเป็น `ไม่ปิด` เพื่อลดโอกาสกด Enter แล้วปิดโดยไม่ตั้งใจ
- หากเลือก `ปิดโปรแกรม` ให้บันทึก Session, URL และรายการ Tab ก่อนออก
- หากเลือก `ไม่ปิด` ให้ยกเลิกการปิดและคง WebView, Login และ Session เดิม
- ไม่แสดงคำถามเมื่อโปรแกรมปิดตัวเองเพื่ออัปเดตหรือ Restart ตามกระบวนการภายใน
- ป้องกัน Confirmation Dialog ซ้อนและไม่ขวาง Windows Shutdown

### Close Confirmation Acceptance Criteria

- กด `X` หรือ `Alt+F4` แล้วโปรแกรมต้องไม่ปิดจนกว่าผู้ใช้จะยืนยัน
- เลือกไม่ปิดแล้ว Tab, URL, Login และหน้าต่างโปรแกรมต้องอยู่ในสถานะเดิม
- เลือกปิดแล้วข้อมูล Session ล่าสุดต้องถูกบันทึกก่อน Process สิ้นสุด
- การติดตั้งอัปเดตต้องปิดและเปิดโปรแกรมกลับได้โดยไม่มี Confirmation Dialog ขวาง Updater

### New Tab Button on the Tab Strip

- แสดงปุ่ม `+` ทางขวาต่อจาก Tab สุดท้ายของทุก Instance
- ปุ่มเลื่อนตามท้ายรายการเมื่อเพิ่ม ปิด Pin หรือสลับลำดับ Tab
- คลิกหรือเลือกด้วย Keyboard แล้วสร้าง Web Tab ใหม่ใน Instance และ Profile ปัจจุบัน
- ใช้เส้นทางเปิด Tab เดียวกับ Toolbar และ `Ctrl+T` โดยไม่ Reload Tab อื่น
- แสดง Tooltip และ Accessible Name ตามภาษาไทยหรือ English
- ปุ่ม `+` ไม่ถูกบันทึกใน Workspace, Session, Backup หรือรายการ Recently Closed

### New Tab Button Acceptance Criteria

- ปุ่ม `+` ต้องอยู่ต่อจาก Tab สุดท้ายและไม่ทับชื่อ Tab หรือปุ่มปิด
- ปุ่มต้องไม่ถูกปิด Pin ทำสำเนา หรือลากสลับกับ Tab จริง
- `Ctrl+Tab` และ `Ctrl+Shift+Tab` ต้องวนเฉพาะ Tab จริง
- การเปิด Tab ผ่านปุ่มต้องไม่กระทบการลากเรียง Pin/Unpin หรือ Focused Tab Highlight

## Completed in v2.1.0

### Export/Import Instance, Tab and Favorites Metadata

- Export ทุก Instance, Instance ปัจจุบัน หรือ Tab ปัจจุบันเป็นไฟล์ `.ewmbackup`
- สำรองชื่อ สี ลำดับ Instance/Tab, Home URL, URL ปัจจุบัน, สถานะเปิด และสถานะ Pin
- Export Favorites ที่สัมพันธ์กับขอบเขตข้อมูลที่เลือก
- เก็บ `manifest.json` และ `metadata.json` ใน Package เดียว
- ตรวจ Product, Backup Format Version, จำนวนรายการ และ SHA-256 ก่อน Import
- Import เป็น Instance, Tab ID และ Profile Folder ใหม่โดยไม่เขียนทับข้อมูลเดิม
- ไม่ Export Cookie, Login, Password, History, Download, Update History หรือ WebView2 Profile

### Metadata Backup Acceptance Criteria

- Export แล้ว Import กลับต้องได้ชื่อ สี ลำดับ URL และสถานะ Pin เหมือนข้อมูลต้นฉบับ
- Import ต้องไม่ลบหรือเขียนทับ Instance, Tab, Favorites หรือ Profile ที่มีอยู่
- ไฟล์ที่ Manifest, จำนวนรายการ หรือ SHA-256 ไม่ถูกต้องต้องถูกปฏิเสธ
- ผู้ใช้ต้องได้รับแจ้งชัดเจนว่า Metadata Backup ไม่รวม Login, Cookie หรือข้อมูลจาก 1Password
- ไฟล์จาก Instance หนึ่งต้องย้ายไปอีกเครื่องได้โดยไม่อ้างอิง Path เฉพาะเครื่องเดิม

## Long-term Direction: Cross-device Sync

> ยังไม่อยู่ในขอบเขตการพัฒนาระยะสั้น เก็บไว้เป็นแนวทางหลังจากระบบ Public Update มีความเสถียรแล้ว

- ใช้แนวทาง Local-first เพื่อให้โปรแกรมทำงานได้ตามปกติแม้ไม่มี Internet
- Sync ชื่อ สี และลำดับ Instance/Workspace
- Sync รายการ Tab, URL, ชื่อ Tab, ลำดับและสถานะ Pin
- Sync Favorites และการตั้งค่าทั่วไปที่ไม่มีข้อมูลลับ
- พิจารณา OneDrive App Folder เป็น Sync Provider แรกโดยขอสิทธิ์เฉพาโฟลเดอร์ของโปรแกรม
- เพิ่ม Device ID, Revision, Updated Time และ Tombstone เพื่อรวมการแก้ไขจากหลายเครื่องโดยไม่ทำให้ Tab หาย
- รองรับ Offline Queue, Sync History และ Conflict Resolution
- เข้ารหัสข้อมูลก่อนอัปโหลด และมี Recovery Key สำหรับเปิดใช้งานในเครื่องใหม่
- ไม่ Sync WebView2 Profile, Cookie, Login Token, Password, Cache, Download Path หรือข้อมูลจาก 1Password
- สำรอง Local Config ก่อน Sync ครั้งแรกและก่อนรวมข้อมูลที่มี Conflict

### Automatic Update Acceptance Criteria

- ผู้ใช้ทั้ง 10 คนตรวจพบเวอร์ชันใหม่ได้จาก Official Public Manifest เดียวกันโดยไม่ต้อง Login
- `Remind me later` และ `Skip this version` ต้องทำงานแยกกันอย่างถูกต้อง
- ห้ามติดตั้งไฟล์ที่ Hash หรือลายเซ็นไม่ถูกต้อง
- หลังอัปเดต Tab, URL, Login, Cookie, Profile และ Session ต้องยังคงอยู่
- ทุกความสำเร็จ ความล้มเหลว การข้ามและ Rollback ต้องแสดงใน Update History
- ไม่รองรับ Custom Update Server, SharePoint, Network Folder, Private Repository หรือ Offline Update Package ใน Roadmap ชุดนี้

## Completed in v2.0.0

### Update Recovery, Rollback and Error Handling

- สำรองไฟล์เวอร์ชันเดิมและสร้าง Recovery Journal ก่อนเขียนไฟล์ใหม่
- ตรวจว่าโปรแกรมใหม่เปิดและโหลด Workspace สำเร็จด้วย Startup Health Check
- Rollback และเปิดเวอร์ชันเดิมกลับอัตโนมัติเมื่อการติดตั้งหรือ Startup ล้มเหลว
- ดาวน์โหลดผ่านไฟล์ `.part` พร้อม Retry อัตโนมัติและตรวจขนาดก่อนตรวจ SHA-256
- ตรวจพื้นที่ว่างและแสดงข้อความแนะนำสำหรับ Network, Timeout, Permission, Disk และ Package Error
- เพิ่ม Update Log และสถานะ `Installed`, `RolledBack`, `RollbackFailed` ใน Update History
- เริ่มใช้ Edge Workspace Manager Community License 1.0 สำหรับ v2.0.0 เป็นต้นไป

## Completed in v1.7.0

### Official Public Update

- ตรวจเวอร์ชันจาก Public GitHub Releases ผ่าน HTTPS โดยไม่ต้อง Login
- แสดง Release Notes และตัวเลือก `Update now`, `Remind me later`, `Skip this version`
- ดาวน์โหลดแบบแสดงความคืบหน้าและตรวจ SHA-256 ก่อนติดตั้ง
- ใช้ Updater แยกเพื่อปิด ติดตั้ง และเปิดโปรแกรมกลับอัตโนมัติ
- บันทึก Tab และ Session ก่ออัปเดต โดยไม่รวม Config หรือ WebView Profile ใน Package
- เพิ่ม Check for updates, Update History และตัวเลือกปิดการตรวจอัปเดตอัตโนมัติ
- GitHub Actions สร้าง ZIP, `update.json`, SHA-256 และ Public Release จาก tag อัตโนมัติ

## Completed in v1.6.1

### Pin Tab Accessibility Fix

- แก้การคลิกขวาบนหัว Tab ให้แสดงเมนู Pin/Unpin และเมนูจัดการ Tab
- เพิ่มปุ่ม `📌` บน Toolbar สำหรับ Pin/Unpin Tab ปัจจุบัน
- เพิ่ม Tooltip และ Accessible Name ตามสถานะของ Tab
- เพิ่มคีย์ลัด `Ctrl+Shift+P`
- ปิดการใช้งานปุ่ม Pin อัตโนมัติเมื่อเลือก External Program Tab

## Completed in v1.6.0

### Bug Fix: Settings Save Button Hidden

- แก้ปุ่ม `Save settings` ถูกดันออกนอกหน้าต่างเมื่อใช้ขนาดเริ่มต้น
- ตรึงแถบปุ่ม Save ไว้ด้านล่างของหน้า Settings ให้มองเห็นตลอดเวลา
- ทำให้ส่วนรายการตั้งค่าเลื่อน Scroll ได้เมื่อพื้นที่แนวตั้งไม่เพียงพอ
- ปรับ Minimum Size และขนาดเริ่มต้นให้เหมาะกับ DPI 100%, 125%, 150% และ 175%
- รองรับภาษาไทยและ English ซึ่งมีความยาวข้อความต่างกัน
- รองรับ Windows Display Scaling และขนาด Font ที่ผู้ใช้ตั้งเอง
- ปุ่ม Save ต้องเข้าถึงได้ด้วย `Tab` และกด `Enter` เพื่อบันทึกได้

### Settings Layout Acceptance Criteria

- เปิดหน้าต่าง Browser Tools ด้วยขนาดเริ่มต้นแล้วต้องเห็นปุ่ม Save ทันที
- ย่อหน้าต่างถึง Minimum Size แล้วปุ่ม Save ต้องยังใช้งานได้
- หากเนื้อหาไม่พอพื้นที่ ต้อง Scroll เฉพาะรายการตั้งค่า ไม่เลื่อนปุ่ม Save ออกจากหน้าจอ
- ทดสอบทั้ง Light/Dark Theme และภาษาไทย/English

### Duplicate Tab Enhancement

- เพิ่มคำสั่ง Duplicate Tab ที่เข้าถึงได้ชัดเจนจากเมนูคลิกขวาและคีย์ลัด
- สร้าง Tab ใหม่ติดกับ Tab ต้นฉบับ ไม่ใช่ท้ายรายการ
- เปิด URL ปัจจุบันของ Tab ต้นฉบับ รวมถึง query string และตำแหน่ง navigation ล่าสุด
- ใช้ Instance/Profile เดียวกันเพื่อรักษา Cookie และ Login
- ตั้งชื่อเริ่มต้นจาก Document Title ของหน้าเดิม
- Duplicate ต้องไม่เปลี่ยน Home URL หรือข้อมูลของ Tab ต้นฉบับ
- รองรับเฉพาะ Web Tab และปิดคำสั่งสำหรับ External Program Tab

### Pin Tab

- เพิ่มคำสั่ง `Pin Tab` และ `Unpin Tab` ในเมนูคลิกขวา
- ย้าย Pinned Tab ไปด้านซ้ายของ Tab ปกติโดยอัตโนมัติ
- แสดง Pinned Tab แบบกะทัดรัด พร้อมสัญลักษณ์หรือ Favicon
- บันทึกสถานะ Pin และลำดับ Pinned Tab แยกตาม Instance
- คืนสถานะ Pin เมื่อเปิดโปรแกรมครั้งถัดไป
- ปุ่ม `×` ไม่แสดงบน Pinned Tab เพื่อลดการปิดพลาด
- `Ctrl+W` บน Pinned Tab ต้องถามยืนยันหรือไม่ปิดตามค่าที่ตั้งไว้
- การลากเรียงต้องไม่ย้าย Tab ปกติแทรกระหว่างกลุ่ม Pinned โดยไม่ตั้งใจ
- รองรับการ Duplicate Pinned Tab โดย Tab สำเนาเริ่มต้นเป็น Tab ปกติ

### Duplicate / Pin Acceptance Criteria

- Duplicate Tab ต้องเปิดหน้าเดียวกับต้นฉบับใน Instance เดิมและไม่ Reload Tab ต้นฉบับ
- Pinned Tab ต้องอยู่ด้านซ้าย จำสถานะหลัง Restart และไม่ถูกปิดโดยคลิกพลาด
- การ Pin, Unpin, Duplicate และลากเรียงต้องไม่กระทบ Login, Session หรือ Tab อื่น

## Completed in v1.5.0

### Dark Mode / Theme

- เลือก Theme จาก Settings: `Light`, `Dark` หรือ `Use Windows setting`
- จดจำ Theme และใช้ค่าเดิมเมื่อเปิดโปรแกรมครั้งถัดไป
- ใช้ Dark Mode กับ Toolbar, Address Bar, Workspace/Tab bar, Status bar, Menu และ Dialog
- ปรับสี Focused Tab และ Instance Color ให้มี Contrast เหมาะสมกับ Theme
- ปรับสีตาราง History, Favorites, Downloads และหน้าจัดการ Tab
- รองรับการเปลี่ยน Theme โดยไม่ Reload WebView หรือทำให้ Login/Session หาย
- ใช้สีระบบและ Palette กลาง เพื่อไม่ให้แต่ละหน้าจอมีโทนสีไม่ตรงกัน
- แยกตัวเลือก `Force dark web pages` ออกจาก Theme ของโปรแกรม และปิดไว้เป็นค่าเริ่มต้น
- แสดงคำเตือนว่า Force Dark อาจทำให้บางเว็บไซต์ รูปภาพ หรือ Print/PDF แสดงสีผิด

### Dark Mode Acceptance Criteria

- ทุกส่วนของ UI ต้องไม่มีพื้นหลังสีขาวจ้าที่หลุดจาก Dark Theme
- ตัวอักษร ปุ่ม `×`, เส้นขอบ และรายการที่เลือกต้องมี Contrast อ่านง่าย
- การเปลี่ยน Theme ต้องไม่สร้าง WebView ใหม่และไม่กระทบ Tab ที่เปิดอยู่
- Theme แบบ `Use Windows setting` ต้องอัปเดตตามการเปลี่ยน Theme ของ Windows

## Completed in v1.4.0

### Focused Tab Highlight

- เน้นสีพื้นหลังของ Tab ที่กำลัง Focus ให้แตกต่างจาก Tab อื่นอย่างชัดเจน
- รองรับทั้ง Web Tab และ Tab ของโปรแกรมภายนอก
- ลดความเด่นของ Tab ที่ไม่ได้เลือก โดยยังอ่านข้อความได้ง่าย
- ใช้สีข้อความดำหรือขาวอัตโนมัติตาม Contrast ของสีพื้นหลัง
- แสดงเส้นขอบหรือแถบสีเพิ่มเติม เพื่อไม่ให้การแยกสถานะพึ่งสีเพียงอย่างเดียว
- มีชุดสี Focus เริ่มต้นที่เข้ากับสีประจำ Instance
- เพิ่มตัวเลือกกำหนด Focus Color หรือกลับไปใช้สีมาตรฐานใน Settings
- อัปเดต Highlight ทันทีเมื่อสลับ Tab ด้วยเมาส์, `Ctrl+Tab` หรือการลากสลับลำดับ
- ไม่ Reload WebView และไม่กระทบ URL, Login หรือ Session

### Focused Tab Acceptance Criteria

- ผู้ใช้ต้องระบุ Tab ที่กำลังใช้งานได้ทันทีโดยไม่ต้องอ่านข้อความทุก Tab
- ชื่อ Tab และปุ่ม `×` ต้องมองเห็นชัดบนสี Focus ทุกสี
- สี Focus ต้องไม่กลืนกับสี Instance หรือสีของ Windows theme

### Address Bar Suggestions / Autocomplete

- แสดงรายการ URL และชื่อหน้าขณะผู้ใช้พิมพ์ใน Address Bar
- ค้นหาจาก History, Favorites และ Tab ที่เปิดอยู่ใน Instance ปัจจุบัน
- ไม่แสดงประวัติจาก Instance อื่น เว้นแต่ผู้ใช้เปิดตัวเลือกค้นหาทุก Instance
- เรียงผลลัพธ์ตามความตรงของข้อความ ความถี่ และเวลาที่เข้าใช้งานล่าสุด
- ใช้ปุ่ม `Up/Down` เพื่อเลือกรายการ, `Enter` เพื่อเปิด และ `Esc` เพื่อปิดคำแนะนำ
- รองรับการคลิกเพื่อเปิด และแสดงชื่อหน้าเหนือ URL ที่ย่อให้อ่านง่าย
- จำกัดจำนวนคำแนะนำเพื่อไม่ให้บดบังพื้นที่หน้าเว็บ
- มีตัวเลือกปิด Address Bar Suggestions ใน Settings
- เมื่อใช้ Private/Temporary Instance จะไม่บันทึกหรือแสดง History Suggestions

### Acceptance Criteria

- คำแนะนำต้องแสดงโดยไม่ทำให้การพิมพ์ Address Bar หน่วง
- ผลลัพธ์เริ่มต้นต้องมาจาก Instance ที่กำลังใช้งานเท่านั้น
- การเลือกคำแนะนำต้องเปิด URL ที่ถูกต้องและไม่สร้าง Tab ซ้ำโดยไม่ตั้งใจ
- การล้าง History ต้องทำให้คำแนะนำจาก History รายการนั้นหายไปทันที

## Completed in v1.3.0

### Rearrange Tabs

- ลากและวางเพื่อสลับลำดับ Tab ภายใน Instance เดียวกัน
- บันทึกลำดับใหม่ลง Workspace config โดยอัตโนมัติ
- รักษา WebView, URL, Login และ Session เดิมระหว่างการย้าย
- รองรับทั้ง Web Tab และ Tab ของโปรแกรมภายนอก

### Rearrange Instances / Workspaces

- ลากและวางเพื่อสลับลำดับ Instance บนแถบด้านบน
- บันทึกลำดับใหม่และใช้ลำดับเดิมเมื่อเปิดโปรแกรมครั้งถัดไป
- ไม่ Reload Tab ภายใน Instance ระหว่างการจัดลำดับ

### Instance Colors

- กำหนดสีประจำ Instance/Workspace แต่ละรายการ
- มีชุดสีแนะนำและรองรับการเลือกสีแบบกำหนดเอง
- แสดงสีบนแถบ Instance เพื่อแยกกลุ่มงานได้รวดเร็ว
- เลือกสีข้อความสีดำหรือสีขาวอัตโนมัติตามความสว่างของพื้นหลัง
- บันทึกสีลง Workspace config และคืนค่าสีเดิมเมื่อเปิดโปรแกรมใหม่
- มีปุ่มล้างสีเพื่อกลับไปใช้สีมาตรฐานของโปรแกรม
- รักษาสีประจำ Instance เมื่อมีการลากสลับลำดับ

### Thai / English Language

- เลือกภาษา `ไทย` หรือ `English` จากหน้า Settings
- บันทึกภาษาที่เลือกและใช้ค่าเดิมเมื่อเปิดโปรแกรมครั้งถัดไป
- แปลข้อความใน Toolbar, Menu, Dialog, Status, Tooltip, Settings และ Error Message
- แปลหน้าข้อมูลเวอร์ชัน รายการอัปเดต และหน้าจัดการ Tab
- จัดเก็บคำแปลใน Resource แยกภาษา เพื่อเพิ่มภาษาอื่นภายหลังได้
- ใช้ English เป็นภาษาสำรองเมื่อไม่พบคำแปล
- เปลี่ยนภาษาได้โดยไม่ทำให้ WebView, Login, Session หรือ Tab ที่เปิดอยู่หาย
- หากทำได้โดยไม่กระทบหน้าต่างที่เปิดอยู่ ให้เปลี่ยนภาษาทันทีโดยไม่ต้อง Restart

### Keep Awake / Presentation Mode

- มีสวิตช์เปิด–ปิดเพื่อป้องกัน Windows Sleep ระหว่างงานที่ต้องเปิดหน้าจอหรือระบบทิ้งไว้
- เลือกได้ว่าจะป้องกันเฉพาะ Sleep หรือป้องกันทั้ง Sleep และการดับจอ
- ตั้งระยะเวลาได้ เช่น 30 นาที, 1 ชั่วโมง, 2 ชั่วโมง หรือจนกว่าจะปิดเอง
- แสดงสถานะ Keep Awake ที่เห็นชัดบนแถบสถานะและ System Tray
- ปิดอัตโนมัติเมื่อครบเวลา เมื่อออกจากโปรแกรม หรือเมื่อผู้ใช้สั่งปิด
- ใช้ Windows Power Request API โดยไม่จำลอง Mouse หรือ Keyboard input
- ไม่แก้ไขหรือปลอม Presence ใน Teams, Slack หรือระบบติดตามการทำงาน
- ไม่ข้าม Group Policy หรือข้อกำหนดด้านพลังงานขององค์กร

## Acceptance Criteria

- ลาก Tab หรือ Instance แล้วตำแหน่งเปลี่ยนทันที
- ปิดและเปิดโปรแกรมใหม่แล้วลำดับยังเหมือนเดิม
- หน้าเว็บและโปรแกรมภายนอกที่เปิดอยู่ไม่ถูกปิดหรือสร้างใหม่
- การลากต้องไม่ทำงานเมื่อกดปุ่มปิด `×` บน Tab
- สีข้อความบน Instance ต้องมี Contrast เพียงพอและอ่านได้ทั้งธีมสว่างและธีมมืด
- หลังเปลี่ยนภาษา ทุกหน้าจอต้องใช้ภาษาเดียวกันและไม่มีข้อความจากอีกภาษาปะปน
- เมื่อปิด Keep Awake เครื่องต้องกลับไปใช้ Power Settings เดิมทันที
