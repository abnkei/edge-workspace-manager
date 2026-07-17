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
- Export ทุก Instance, Instance ปัจจุบัน หรือ Tab ปัจจุบันพร้อม Favorites เป็นไฟล์ `.ewmbackup`
- Import Metadata Backup เป็น Instance และ Profile ใหม่โดยไม่เขียนทับข้อมูลเดิม
- ตรวจ Manifest, จำนวนรายการ และ SHA-256 ก่อน Import โดยไม่รวม Cookie, Login, Password หรือ WebView2 Profile
- ถามยืนยันก่อนปิดโปรแกรมด้วยปุ่ม `X` หรือ `Alt+F4` และบันทึก Session ก่อนออก
- ปุ่ม `+` ต่อจาก Tab สุดท้ายสำหรับเปิด Tab ใหม่ใน Instance ปัจจุบัน

## Build เป็น EXE

1. ติดตั้ง .NET 8 SDK บน Windows
2. แตก ZIP
3. ดับเบิลคลิก `build.bat`
4. ไฟล์จะอยู่ที่ `releases\v2.1.1\EdgeWorkspaceManager.exe`

เลขเวอร์ชันจะแสดงบนแถบสถานะด้านล่างของโปรแกรม กดที่เลขเวอร์ชันเพื่อดูรายการอัปเดตทั้งหมด

## คิวอัปเดตถัดไป

ยังไม่ได้กำหนดเวอร์ชันถัดไป โดย Import Preview, Incognito, Live Theme Switching, Full Local Backup และ Update Channels ถูกเลื่อนออกไปก่อน

รายละเอียดและเงื่อนไขการทดสอบอยู่ใน `ROADMAP.md`

ระหว่าง Build จำเป็นต้องเชื่อมต่ออินเทอร์เน็ตครั้งแรกเพื่อดาวน์โหลด Microsoft.Web.WebView2 package

## License

| Version | License |
|---|---|
| v1.7.4 and earlier | [MIT License](licenses/MIT-v1.7.4-and-earlier.txt) |
| v2.0.0 and later | [Edge Workspace Manager Community License 1.0](LICENSE) |

Edge Workspace Manager ตั้งแต่ v2.0.0 เป็นต้นไปเผยแพร่ภายใต้ [Edge Workspace Manager Community License 1.0](LICENSE) Copyright (c) 2026 Thanakhan Pariput ซึ่งเป็น Source-available License และไม่ใช่ OSI-approved Open Source License

License อนุญาตให้ใช้งานส่วนบุคคล การศึกษา งานวิจัย และใช้งานภายในองค์กรได้ฟรี รวมถึงอนุญาตให้ผู้ให้บริการใช้ Official Binary เป็นเครื่องมือประกอบในระบบ RPA, VDI, Hosting หรือ Managed Service ที่มีคุณค่าหลักมาจากระบบโดยรวม ผู้ให้บริการสามารถคิดค่าระบบ Infrastructure, Automation, Integration และการดำเนินงานได้ แต่ห้ามแยกขายหรือกำหนดราคาโดยอิง License, User, Workspace, Tab, Installation, Support หรือ Training ของ Edge Workspace Manager และห้ามนำ Source Code หรือ Derivative Work ไปสร้างผลิตภัณฑ์หรือบริการเชิงพาณิชย์โดยไม่มี Commercial License

รุ่นที่เคยเผยแพร่ภายใต้ MIT License ยังคงได้รับสิทธิ์ตาม MIT License ของรุ่นนั้น การเปลี่ยน License ไม่มีผลเพิกถอนสิทธิ์ที่ให้ไปแล้ว

### Contributor License Agreement (CLA)

ผู้ร่วมพัฒนายังคงเป็นเจ้าของลิขสิทธิ์ใน Code และเอกสารที่ตนส่งเข้ามา แต่การส่ง Pull Request เพื่อรวมเข้าโครงการถือเป็นการให้สิทธิ์แก่ผู้ดูแลโครงการตาม Section 10 ของ [Community License](LICENSE) รวมถึงสิทธิ์ในการใช้งาน แก้ไข แจกจ่าย ออก License ใหม่ และออก Commercial License สำหรับ Contribution นั้น

โครงการใช้ CLA เพื่อให้สามารถดูแล Community Edition ภายใต้เงื่อนไขเดียวกัน ออก Commercial License ให้ผู้ที่ต้องการสิทธิ์เพิ่มเติม และบริหารโครงการได้อย่างต่อเนื่องในระยะยาว โดย CLA ไม่ได้โอนความเป็นเจ้าของ Contribution ออกจากผู้ร่วมพัฒนา และผู้ร่วมพัฒนายังคงสามารถนำผลงานของตนไปใช้งานหรือให้ License เพื่อวัตถุประสงค์อื่นได้

ก่อนส่ง Pull Request ผู้ร่วมพัฒนาต้องมีสิทธิ์ใน Contribution นั้น และหากเป็นผลงานที่สร้างระหว่างการจ้างงาน ต้องได้รับอนุญาตจากนายจ้างหรือองค์กรที่เกี่ยวข้องก่อน

### Commercial License

หากต้องการนำ Source Code หรือ Derivative Work ไปใช้ในผลิตภัณฑ์หรือบริการเชิงพาณิชย์ สร้างบริการที่มีคุณค่าหลักจากโปรแกรม แจกจ่าย Binary, Rebrand โปรแกรม หรือขอสิทธิ์ที่ Community License ไม่อนุญาต กรุณาติดต่อ [abnkei@gmail.com](mailto:abnkei@gmail.com?subject=Edge%20Workspace%20Manager%20Commercial%20License)

โปรดระบุชื่อผู้ติดต่อ ชื่อองค์กร รูปแบบการใช้งาน จำนวนผู้ใช้หรือลูกค้า รูปแบบการติดตั้งหรือเผยแพร่ และสิทธิ์ที่ต้องการ การส่งคำถามไม่ได้ให้สิทธิ์เชิงพาณิชย์ สิทธิ์ดังกล่าวจะเกิดขึ้นต่อเมื่อทั้งสองฝ่ายอนุมัติข้อตกลงเป็นลายลักษณ์อักษรแยกต่างหากแล้วเท่านั้น

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
