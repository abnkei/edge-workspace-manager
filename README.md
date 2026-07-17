# Edge Workspace Manager — Tabbed Workspace

โปรแกรม Windows สำหรับรวมเว็บไซต์หลายระบบไว้ในโปรแกรมเดียว โดยแบ่งเป็นกลุ่ม Workspace และ Tab ภายในแต่ละกลุ่ม

## รูปแบบ

- Workspace / Instance 1
  - Tab 1
  - Tab 2
- Workspace / Instance 2
  - Tab 1

แต่ละเว็บไซต์เปิดอยู่ภายในโปรแกรมด้วย Microsoft Edge WebView2 ไม่เปิดหน้าต่าง Edge แยกออกไป

## ความสามารถ

- รวมหลายเว็บไซต์เป็น Tab
- มีกลุ่ม Workspace หลายชุด
- แยก Cookie, Login และ Session ด้วย Profile Folder ของแต่ละ Workspace
- Back / Forward / Refresh / Home
- แก้ URL จาก Address Bar
- เพิ่ม แก้ไข ลบ Workspace และ Tab
- บันทึกค่าที่ `%LocalAppData%\EdgeWorkspaceManager\workspaces.json`
- Smart Search: พิมพ์ URL หรือคำค้นหาในช่องเดียวกัน
- คำค้นหาลัด `g`, `b`, `yt` และ `maps` เช่น `yt เพลงทำงาน`
- เปิด ปิด ทำสำเนา ปิด Tab อื่น และปิด Tab ด้านขวา
- คืน Tab ที่ปิดล่าสุด และคืน Session เมื่อเปิดโปรแกรมอีกครั้ง
- เปิดลิงก์แบบ Popup หรือ `target="_blank"` เป็น Tab ใหม่
- คีย์ลัดแบบ Browser: `Ctrl+L`, `Ctrl+T`, `Ctrl+W`, `Ctrl+Shift+T`, `Ctrl+Tab`, `Ctrl+R`, `Ctrl+F` และ `Alt+Left/Right`
- Favorites และ History แยกตาม Workspace
- Download Manager พร้อม Pause, Resume, Cancel และเปิดไฟล์/โฟลเดอร์
- Find in Page, Zoom, Print Preview และ Save PDF
- Settings สำหรับ Search Engine, Download Folder, DevTools และจำนวน History
- ลากสลับลำดับ Tab และ Instance พร้อมบันทึกลำดับ
- กำหนดสีประจำ Instance
- เลือกภาษาไทยหรือ English จาก Settings
- Keep Awake ป้องกัน Sleep พร้อมตั้งเวลาปิดอัตโนมัติ
- Address Bar Suggestions จาก History, Favorites และ Tab ที่เปิดอยู่
- Focused Tab Highlight พร้อมกำหนดสีและ Contrast อัตโนมัติ
- Theme แบบ Light, Dark หรือใช้ค่าจาก Windows
- Force dark web pages แบบเลือกเปิดได้จาก Settings
- Pin/Unpin Tab จาก Toolbar, เมนูคลิกขวา หรือ `Ctrl+Shift+P`
- Official Public Update ผ่าน GitHub Releases พร้อม SHA-256 verification
- Update now, Remind me later, Skip this version และ Update History

## Build เป็น EXE

1. ติดตั้ง .NET 8 SDK บน Windows
2. แตก ZIP
3. ดับเบิลคลิก `build.bat`
4. ไฟล์จะอยู่ที่ `releases\v1.7.4\EdgeWorkspaceManager.exe`

เลขเวอร์ชันจะแสดงบนแถบสถานะด้านล่างของโปรแกรม กดที่เลขเวอร์ชันเพื่อดูรายการอัปเดตทั้งหมด

## คิวอัปเดตถัดไป

- v1.7.1: Update Recovery, Rollback และ Error Handling
- v1.8.0: Stable/Beta Channel, Background Download และ Critical Update Policy

รายละเอียดและเงื่อนไขการทดสอบอยู่ใน `ROADMAP.md`

ระหว่าง Build จำเป็นต้องเชื่อมต่ออินเทอร์เน็ตครั้งแรกเพื่อดาวน์โหลด Microsoft.Web.WebView2 package

## License และ Third-party software

Edge Workspace Manager เผยแพร่ภายใต้ [MIT License](LICENSE) Copyright (c) 2026 Thanakhan Pariput

โปรแกรมใช้ Microsoft Edge WebView2 และรวมส่วนประกอบของ Microsoft .NET ไว้ใน Release แบบ self-contained ดูรายละเอียด License ของส่วนประกอบภายนอกได้ที่ [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)

## หมายเหตุ

โปรแกรมรุ่นนี้รองรับหน้าเว็บไซต์เป็นหลัก หากต้องการนำหน้าต่างโปรแกรมอื่น เช่น SAP GUI, Excel หรือ Power Automate Desktop มาใส่ใน Tab ต้องใช้ Windows API แบบฝังหน้าต่าง ซึ่งมีข้อจำกัดและควรพัฒนาเป็นโหมดเพิ่มเติมแยกต่างหาก

## การลากโปรแกรมภายนอกเข้ามาเป็น Tab

1. เปิดโปรแกรมภายนอกที่ต้องการ เช่น Excel, Notepad หรือ SAP GUI
2. ใน Edge Workspace Manager กดปุ่ม `⊕` ค้างไว้
3. ลากเมาส์ไปปล่อยบนหน้าต่างโปรแกรมภายนอก
4. หน้าต่างดังกล่าวจะถูกนำเข้ามาแสดงเป็น Tab ใน Workspace ปัจจุบัน
5. คลิกขวาที่ชื่อ Tab แล้วเลือก **นำหน้าต่างออกจาก Tab** เพื่อคืนหน้าต่างเป็นแบบปกติ
6. ปุ่ม `×` หรือ `Ctrl+W` บน Tab ภายนอกจะคืนหน้าต่างสู่ Desktop โดยไม่ปิดโปรแกรม
7. หากต้องการปิดโปรแกรมจริง ให้คลิกขวาที่ Tab แล้วเลือก **ปิดหน้าต่างโปรแกรม...** และยืนยัน

ข้อจำกัด: โปรแกรมแบบ UWP, โปรแกรมที่รันสิทธิ์ Administrator สูงกว่า, โปรแกรมที่ใช้หน้าต่างหลาย Process หรือมีระบบป้องกันการฝัง อาจไม่รองรับ ฟังก์ชันนี้จับหน้าต่างที่กำลังเปิดอยู่และจะไม่บันทึก Handle ข้ามการ Restart โปรแกรม
