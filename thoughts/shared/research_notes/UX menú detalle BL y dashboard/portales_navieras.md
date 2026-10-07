# Information architecture of carrier and logistics customer portals (navigation, shipment/BL detail, release readiness, dashboards)

Research date: 2026-10-06. Method note: almost every carrier site (hapag-lloyd.com, cma-cgm.com, msc.com, maersk.com FAQ pages, support.project44.com) returned HTTP 403 or header errors to direct fetching. Findings therefore come from (a) search-engine snippets of official pages, (b) two official PDFs downloaded and text-extracted locally (the Hapag-Lloyd Navigator 2.0 User Guide, 18 pages, and the Maersk Malaysia Import Booklet, 10 pages), and (c) the few pages that did load (ShipmentLink home, ONE advisory, Flexport blog/video pages). Nothing was verified from a logged-in session. No screenshots were inspected.

## Q1. How many top-level menu items do these portals use, and how are tools grouped?

### Takeaway
The main carriers group functions by **shipment lifecycle / task** (Prepare/Book → Document → Track/Monitor → Import → Pay/Finance). The groupings range from 3 large task-based buckets (CMA CGM) to 14 flat items (Evergreen ShipmentLink). Hapag-Lloyd and Maersk sit in between and wrap a shipment-centric "binder" (Navigator / Shipment Binder) around those stage-based tools. Forwarders and visibility platforms (Flexport, project44) are **object-centric**: a shipment list or dashboard plus a left-side menu within each shipment.

### Cited Findings
**CMA CGM (My CMA CGM)**
- The "My CMA CGM" menu has task-phrased groups: "Prepare your Shipment" (routing finder, booking), "Monitor your Shipment" (shipment dashboard, tracking) and "Manage your Finance" (invoice dashboard, "Manage your invoices"). — [CMA CGM finance page / search snippets](https://www.cma-cgm.com/products-services/ecommerce/finance); [My CMA CGM](https://monitor.cma-cgm.com/my-cma-cgm)
- "Monitor your Shipment" was introduced to replace the earlier "My eBusiness Center" entry, so that tracking would be simpler. This was a deliberate IA change from a product-named bucket to a task-named one. — [My CMA CGM (search snippet)](https://monitor.cma-cgm.com/my-cma-cgm)
- Tracking is reachable both from the home page and from main menu > "My CMA CGM". — [CMA CGM container tracking (snippet)](https://monitor.cma-cgm.com/products-services/ecommerce/container-tracking)

**Evergreen (ShipmentLink)**
- The home page shows 14 top-level items: All-in-One, Booking, Demurrage & Detention, My Export, My Import, My Payment, VGM, Tracking, Reefer Online Service, N. American Information, e-Report, Sailing Schedules, EDI/API, Location. — [ShipmentLink](https://www.shipmentlink.com/) (fetched)
- Submenus are grouped by direction and finance. My Export = B/L Instruction, Supplementary EU ENS Data, Proofreading, Non-negotiable B/L, i-B/L, i-Dispatch, eBL. My Import = Arrival Notice, Import Door Delivery, US Importer Security Filing. My Payment = Freight Details, i-Invoice, e-Payment. Tracking = Cargo Tracking, Member Tracking, EverDRY SMART. — [ShipmentLink](https://www.shipmentlink.com/)
- Note: D&D gets its own top-level item, separate from Import and Payment. — [ShipmentLink](https://www.shipmentlink.com/)

**Hapag-Lloyd (Online Business Suite / Navigator 2.0)**
- The Online Business Suite is organized into stage-based sections, visible in URL paths: /online-business/quotation/…, /import/…, /track/…, /navigator/…, plus documents/shipping-instructions. — [Online Business Suite](https://www.hapag-lloyd.com/en/online-business.html); [Import Overview](https://www.hapag-lloyd.com/en/online-business/import/import-overview-solution.html); [Track](https://www.hapag-lloyd.com/en/online-business/track/track.html)
- Navigator 2.0 has two list tabs ("Shipments" and "To Do's"), a shared search box, and a shipment-details view with four menu sections: Overview, Containers and Cargo, Documents, Additional Services. — [Navigator 2.0 User Guide PDF](https://www.hapag-lloyd.com/content/dam/website/downloads/pdf/HLAG_Navigator_2.0_User_Guide.pdf) (text extracted)
- "My Shipments" only shows bookings in which the user's assigned company is involved. — [OLB User Guide: My Shipments (snippet)](https://www.hapag-lloyd.com/en/online-business/olb-user-guide/shipments/my-shipments.html)

**Maersk (maersk.com hub)**
- The top menu contains a "Manage" entry, under which sits "Shipment Overview – Export" (and an import counterpart, "Import Shipment Overview"). — [Maersk FAQ: Shipment Overview (snippet)](https://www.maersk.com/support/faqs/access-shipment-overview); [Maersk Import Booklet PDF](https://www.maersk.com/~/media_sc9/maersk/news/advisories/files/2022/11/maersk-import-booklet-my-v4.pdf)
- Maersk markets a "Logistics Hub" as one dashboard where customers can "check rates, manage invoices, book, and track shipments". Finance lives in a separate "MyFinance" area. — [Maersk Logistics Hub](https://www.maersk.com/digital-services/logistics-hub); [Import Booklet](https://www.maersk.com/~/media_sc9/maersk/news/advisories/files/2022/11/maersk-import-booklet-my-v4.pdf)
- Notifications are set up from a user/profile icon menu ("Click on [icon] > Notifications"). — [Import Booklet](https://www.maersk.com/~/media_sc9/maersk/news/advisories/files/2022/11/maersk-import-booklet-my-v4.pdf)

**MSC (myMSC)**
- Advertised features include quoting, booking, shipping instructions, VGM, tracking by BL/booking/container, vessel schedules, and visibility of MSC shipments booked through INTTRA, GT Nexus or CargoSmart. — [MSC eBusiness](https://www.msc.com/en/ebusiness); [myMSC App Store](https://apps.apple.com/us/app/mymsc/id1454791941)
- The myMSC Dashboard has a **drop-down menu in the top-left corner**. One path is "Free Time, Detention & Demurrage" → "Invoice Dispute". — [MyMSC Invoice Dispute Portal PDF (snippet)](https://www.msc.com/-/media/files/msc-cargo/local-information/america/united-states/mymsc---ddp-invoice-dispute.pdf?rev=-1&hash=1984FA60AAB4AD1CAB2351A804AFFF64)
- Payments and invoices run on a separate subdomain (e-pay.mymsc.com). — [myMSC e-Pay](https://e-pay.mymsc.com/Invoice/GetBOLInvoice)

**ONE (Ocean Network Express eCommerce)**
- Features sit under a "Manage Shipment" area (e.g. /one-ecom/manage-shipment/cargo-tracking; "eCommerce Manage Shipment – Import Shipment Overview"). — [ONE Cargo Tracking](https://www.one-line.com/one-ecom/manage-shipment/cargo-tracking); [ONE advisory](https://www.one-line.com/en/news/customer-advisory-import-shipment-overview)

**Flexport**
- Navigation is built around a personalized dashboard plus a Shipments list. In the shipment page redesign, "the tabs from the shipment page now live in the menu on the left side". The Shipments list supports saved shipment groups and filters for Status, Date, Location, Company, Issue, Mode and Assigned To. — [Flexport Help: platform updates Sept 2019 (snippet)](https://www.flexport.com/help/962-new-updates-to-the-flexport-platform-september-2019/); [Flexport video: new shipment page](https://www.flexport.com/videos/shipment-details/)

### Inferences
- Phrasing groups as tasks ("Prepare / Monitor / Manage your finance") is the most recent carrier pattern (CMA CGM). Flat product menus with 10+ items (Evergreen) are the legacy pattern.
- Most carriers split import from export (Maersk "Shipment Overview – Export" vs "Import Shipment Overview", Evergreen "My Export / My Import", ONE "Import Shipment Overview", Hapag-Lloyd "Import Overview"). That split matters for an import-release-focused portal.
- Finance and payment is usually a separate area, sometimes on a separate subdomain (MSC e-pay, Maersk MyFinance, CMA CGM "Manage your Finance").

### Gaps
- Exact current top-level menu item counts for Maersk, MSC, ONE and Hapag-Lloyd (logged-in header) could not be verified. All pages were blocked or required login.
- No source found describing how carriers separate customer and internal/admin functions. Public docs only describe role/party-based visibility (Hapag-Lloyd company scoping, document download by party role). Admin consoles are not publicly documented.
- No published redesign case study (Behance/agency) was found for any carrier in this session.

## Q2. What does the shipment/BL detail page look like?

### Takeaway
The dominant pattern is a **persistent summary header plus a few tabs or left-menu sections** (Overview / Containers & Cargo / Documents / Services or Finance), with a **routing/milestone visual** at the top. Completed events are highlighted and future events greyed. Flexport is the most documented example of a single page with a central vertical timeline, a right-hand task/exception inbox and a left menu.

### Cited Findings
**Hapag-Lloyd Navigator 2.0 (user guide, most detailed source found)** — [Navigator 2.0 User Guide PDF](https://www.hapag-lloyd.com/content/dam/website/downloads/pdf/HLAG_Navigator_2.0_User_Guide.pdf)
- **Shipments list:** default columns are Booking No., Your Reference, Bill of Lading No., Main Vessel, Voyage No, Start Location, ETD, End Location, ETA. The list offers "13 column items and 12 options to show or not", and the column view persists to the next session. Other controls:
  - Download to .xlsx, which respects active filters but not column customization.
  - Sorting on one column at a time.
  - Filters by location, vessel and ETA/ETD date range.
  - Indicators for additional products bought or available per shipment, plus transit time, and a "BUY Additional Services" button.
  - Paging that is kept when the user returns from a detail view.
- **Search:** one box shared by both tabs. It accepts Booking, BL, Container or Invoice number and shows results below the search bar.
- **Detail page (opened from Shipments or To Do's):**
  - A back button to the last tab.
  - A routing section that "will be kept once you change tabs" (a persistent header). It shows port connections and vessel name/voyage, with hover details on icons. "Orange color will be visible once the event occur".
  - Menu options to switch sections.
  - A To Do's section that flags missing VGM or SI with the deadline and links straight to the task.
  - A summary of container and cargo details and document status, with links to the matching tab.
- **Overview tab:** customer reference; status of some documents; purchased products (Additional Freetime, Shipping Guarantee, HL Live); future cut-off dates, plus past cut-offs up to one week back; containers, commodity and type of movement; shipment actions; port details (address, contact, opening times); vessel details (transit time, voyage number). Every completed event is shown "in bold (orange)" and the next events in grey.
- **Containers and Cargo tab:** container details with seals and reefer data. Clicking a container number opens a pop-up with that container's details. Cargo details include HS code and DG data. Each container has its own tracking and transport events, and the tab shows the depot for empty pick-up or redelivery in one view.
- **Documents tab:** a table of document, who can download it, and when. Booking Confirmation is for the booking sender/creator. Invoice is for the payer (Prepaid/Collect). Arrival Notice is for the Notify party. Sea Waybill is for the booking sender/creator, shipper and consignee. Documents become available once created in HL systems while the booking is active and within the date range. Otherwise a message explains that the document is not created yet or the user is not a party to the shipment.
- **Additional Services tab:** shows purchased Shipping Guarantee, Additional Freetime and HL Live, and lets users buy Shipping Guarantee and Additional Freetime.
- **Feedback** widget on the right side of the screen.
- Navigator is described as a "customizable detailed overview of your active shipments with multiple filters" with "easy navigation to container and cargo details, documents, and additional services". — [Navigator 2.0 page (snippet)](https://www.hapag-lloyd.com/en/online-business/navigator/hapag-lloyd-navigator.html)
- Invoices are downloaded from the shipment's Documents tab, but only by the payer. — [Hapag-Lloyd FAQ (snippet)](https://www.hapag-lloyd.com/en/services-information/offices-localinfo/europe/netherlands/local-info/faq.html)

**Maersk (Shipment details / "Shipment Binder")**
- Each shipment in "My Shipments" links to a "digital binder" containing booking and documentation details, container information, transport plan, price information and links to related documents. — [Maersk FAQ: Shipment Overview (snippet)](https://www.maersk.com/support/faqs/access-shipment-overview)
- Shipment details have a top toolbar under the shipment number. "Documents" is one entry and splits into "Export related documents" and "Import related documents". The delivery order is downloaded from "Import related documents". There is also a "Shipment Overview" tab with a "View Delivery Details" action. — [Maersk FAQ: Delivery order/container release confirmation (snippet)](https://www.maersk.com/support/faqs/where-can-i-find-my-delivery-order-or-container-release-confirmation); [Maersk search snippets](https://www.maersk.com/support/faqs/how-can-i-submit-my-import-container-release-instructions)
- A **"Bill of Lading" section on the right side** of shipment details shows release prerequisite status indicators (see Q3). — [Maersk FAQ: Submit Import Delivery Order (snippet)](https://www.maersk.com/support/faqs/how-can-i-submit-my-import-delivery-order)
- The Shipping Instructions flow has five tabs: Document, Parties, Payers, Cargo and VGM, Review. — [Maersk SI article (snippet)](https://www.maersk.com/news/articles/2023/11/15/shipping-instructions)
- A downloadable "Transport Plan Change Notice" (July 2026) gives booking-specific transport plan updates and disruption details. — [Maersk news 2026-07-08](https://www.maersk.com/news/articles/2026/07/08/transport-plan-change-notice-downloadable-document)

**CMA CGM (Shipment dashboard)**
- The dashboard is a "centralized view of your cargo and your containers" with direct access to all related documents. It has three default views: "all Import/Export shipments", "SI Dashboard" and "view by container". — [CMA CGM shipment dashboard (snippet)](https://www.cma-cgm.com/my-cma-cgm/shipment-dashboard)
- The search bar accepts POL, POD, container, transport reference and more. All documents are stored together and can be downloaded. — [same](https://www.cma-cgm.com/my-cma-cgm/shipment-dashboard)

**Flexport (shipment page, the most explicit UX write-up found)** — [Flexport video page "The new shipment page: details and exceptions"](https://www.flexport.com/videos/shipment-details/)
- **Header:** shipment name, Flex ID, mode, containers, customs entry, a plain-language status (e.g. "in transit to arrival port"), and in the upper right "Flexport predicted delivery" with a date and countdown.
- **Center:** a vertical timeline of operational milestones from booking to delivery. Estimates turn into actual dates as they happen. Legs expand to show vessel, transit time and container status. Customs milestones appear on the same page.
- **Right:** a persistent "shipment inbox" with open tasks pinned on top with due dates, exception messages (e.g. delays releasing bills of lading) and threaded chat with operators.
- **Left menu:** Documents (searchable, in-app preview), Details and Services (quote/invoice info, POs, packing lists), Cost and Billing (quote and invoice combined), Event History (searchable feed with date filter).
- The page adapts to ocean vs. air and hides customs sections when Flexport does not handle clearance.
- Caveat: the page fetch reported "Published: September 29, 2026", which looks unreliable. The redesign is more likely from around 2019–2020, consistent with the Sept 2019 help article and the June 2020 blog. Treat the date as unverified.

**project44 Movement**
- The help center has a "Navigating Shipment Details" section with articles such as "Shipment Details Page Basics" and "Related Shipments – 'Return to Sender'". It also documents "Container Statuses and Shipment Milestones". — [project44 support (snippet)](https://support.project44.com/hc/en-us); [Container Statuses and Shipment Milestones](https://support.p-44.com/hc/en-us/articles/4812871209499-Container-Statuses-and-Shipment-Milestones)
- The tracking data model separates events/milestones, positions, and route segments/stops (API endpoints Get Event History, Get Position History, Get Route Information, Get Tracking History). — [project44 Developer Portal](https://developers.project44.com/api-reference/api-docs/shipment:-tracking/getshipmenttrackinghistory)

### Inferences
- The common skeleton is: persistent header (IDs + route + vessel/voyage + ETA/ETD + status) → tabs or sections (Overview | Containers & Cargo | Documents | Charges/Finance | Services) → per-container drill-down (pop-up or expandable row) with its own events.
- Documents are gated by **party role** (Hapag-Lloyd table, Maersk payer logic). The UI explains why a document is missing instead of hiding it.
- Milestones use a "done = highlighted, next = grey" convention (Hapag-Lloyd orange/bold; Flexport estimate → actual).

### Gaps
- Maersk's exact tab list on the current shipment details page (beyond "Shipment Overview" and "Documents") could not be verified.
- No screen-level description of myMSC, ONE or ShipmentLink BL detail pages was found.
- project44 "Shipment Details Page Basics" content was blocked (403).

## Q3. How do they surface cargo release / import readiness (holds, D&D, payment status)?

### Takeaway
Carriers turn release readiness into a **checklist of prerequisite statuses**, shown as green/not-green indicators or dashboard columns: BL surrendered/issued, manifest/customs, prepaid/collect charges paid, arrival notice, free time/last free day. Each checklist is gated by a single primary action ("Request Delivery Order"). Hapag-Lloyd and CMA CGM add explicit **To-do / pending tasks** lists with priority or deadlines.

### Cited Findings
- **Maersk:** the "Bill of Lading" section on the right side of shipment details must show four items **green** before the user can click "Request Delivery Order": "Waybill Issued", "Import Manifest Submitted to Customs", "Prepaid Charges: Payment Completed" and "Collect Charges: Payment Completed". The FAQ also says "Bills of Lading surrendered and Prepaid charges payment must be green before proceeding". — [Maersk FAQ: Submit Import Delivery Order (snippet)](https://www.maersk.com/support/faqs/how-can-i-submit-my-import-delivery-order); [Maersk FAQ: container release](https://www.maersk.com/support/faqs/release-of-container)
- **Maersk:** release depends on "local customs regulations, charges being paid and all relevant documents surrendered". Once cleared, a release or delivery order request is sent from the Shipment details page. — [Maersk FAQ: Submit import container release instructions (snippet)](https://www.maersk.com/support/faqs/how-can-i-submit-my-import-container-release-instructions)
- **Maersk import booklet (Malaysia):** "You may check Cargo Release Status and Finance Status here" inside the DO flow. DO submission is only possible once the BL has been surrendered and, at certain ports, from 2 days before ETA. The DO form covers:
  - release type (merchant haulage vs carrier haulage/store door) and container selection
  - a payment-proof upload prompt "if Collect Charges payment status are pending"
  - an optional "Detention details" panel showing **estimated D&D charges**
  - a case number on confirmation

  D&D gets "one-click online visibility of free days & last free date", and changing the planned empty return date re-estimates detention. — [Maersk Import Booklet PDF](https://www.maersk.com/~/media_sc9/maersk/news/advisories/files/2022/11/maersk-import-booklet-my-v4.pdf) (2022; Malaysia-specific)
- **Maersk public import check (no login):** shows ETA/discharge date, vessel/voyage, "Bill of lading type and its status", local charges with breakdown, "Free time and DO readiness", and container type and count. — [Maersk article "How to view & track Import Shipment Details" (snippet; 2020)](https://www.maersk.com/news/articles/2020/11/13/how-to-view-and-track-import-shipment-details)
- Maersk has a dedicated FAQ (Sept 2025) titled "Where can I check my import freight release status". Its content could not be fetched. — [Maersk FAQ 2025-09-05](https://www.maersk.com/support/faqs/2025/09/05/where-can-i-check-my-import-freight-release-status)
- **Hapag-Lloyd Import Overview:** shows "cargo, container and bill of lading details as well as release, customs, rate of exchange and redelivery information". D&D can be checked per shipment or container via "My Shipment" or the "Import Overview" section. — [Import Overview](https://www.hapag-lloyd.com/en/online-business/import/import-overview-solution.html); [HL D&D insight (snippet)](https://www.hapag-lloyd.com/en/online-business/digital-insights-dock/insights/2024/06/detention-and-demurrage--what-is-the-d-d-charge-in-shipping---.html)
- **Hapag-Lloyd Navigator To Do's:**
  - Pending tasks (currently SI or VGM missing) are listed in their own tab, filterable by task type and sorted by "To Do." by default.
  - Each task carries a priority badge: "!Overdue – Cut off is passed already", "High – Less than 3 days till cut off", "Medium – Less than 6 days", "Low – 6 or more days".
  - The same To-Do block appears on the shipment detail page and links to the task.

  These to-dos are export-side. No import-release to-dos are documented. — [Navigator 2.0 User Guide PDF](https://www.hapag-lloyd.com/content/dam/website/downloads/pdf/HLAG_Navigator_2.0_User_Guide.pdf)
- **ONE Import Shipment Overview** (Dec 2022): an import dashboard showing "Arrival notice readiness", "Payment & Invoice availability" and "Manifest and Surrender information", with self-service Print, Email and Chat. One version covers all regions except North America, and a separate version includes North America. — [ONE advisory](https://www.one-line.com/en/news/customer-advisory-import-shipment-overview); [ONE incl. North America](https://ch.one-line.com/en/news/ecommerce-manage-shipment-import-shipment-overview-including-north-america)
- **CMA CGM:** the shipment dashboard has a **"To do" column** that "tells you the next steps to take". Electronic signature of the letter of instructions enables "Express Release" (documents sent immediately to the agent). — [CMA CGM shipment dashboard (snippet)](https://www.cma-cgm.com/my-cma-cgm/shipment-dashboard)
- **MSC:** in-transit features include arrival notices, "advisories of charges" and non-negotiable BL copies. D&D sits in a dashboard menu group "Free Time, Detention & Demurrage" with "Invoice Dispute". — [MSC USA (snippet)](https://www.msc.com/en/local-information/america/united-states); [Invoice Dispute PDF (snippet)](https://www.msc.com/-/media/files/msc-cargo/local-information/america/united-states/mymsc---ddp-invoice-dispute.pdf?rev=-1&hash=1984FA60AAB4AD1CAB2351A804AFFF64)
- **Evergreen:** D&D is its own top-level menu item ("Demurrage & Detention", fee calculation). Import has Arrival Notice and Import Door Delivery. — [ShipmentLink](https://www.shipmentlink.com/)
- **Flexport:** release-related exceptions (e.g. "delays in releasing bills of lading") and open tasks with due dates are pinned in the shipment inbox. — [Flexport video page](https://www.flexport.com/videos/shipment-details/)

### Inferences
- The best-documented release UX (Maersk) is a **named checklist of 4 binary conditions with traffic-light colors right next to the CTA**. That is a strong pattern to reuse for a "requisitos de liberación" view.
- Prioritizing by deadline (Hapag-Lloyd's Overdue/High/Medium/Low) can be reused for import deadlines such as last free day and payment due.
- D&D is shown as free days, last free date and an estimated cost that changes with the planned return date (Maersk).

### Gaps
- No public source shows how holds (customs hold, freight hold, line hold) are labelled in carrier UIs. Maersk lists prerequisites rather than "holds".
- Hapag-Lloyd's Import Overview screen layout (fields and release status labels) could not be fetched.
- The current myMSC release/BL status display was not found.

## Q4. What do home dashboards show and how much?

### Takeaway
Carrier home dashboards are mostly **lists with filters and a task/to-do layer** (Hapag-Lloyd Navigator Shipments + To Do's; CMA CGM views + To do column; ONE import dashboard with readiness columns). Flexport and Maersk market widget-style dashboards (map, tasks, exceptions, shipment list, custom widgets).

### Cited Findings
- **Flexport** (June 11, 2020): a personalized dashboard pre-configured for three personas, with a world map of shipment routes, a Tasks panel, an Exceptions panel, a shipment list, and drag-and-drop customizable widgets. New modules at the time were a Carbon Emissions Estimator, a Duty Drawback Estimator, COVID-19 Updates, Shipments and Industry News. — [Flexport blog](https://www.flexport.com/blog/platform-updates-put-more-control-in-users-hands-and-create-a-personalized/) (dated 2020, may be outdated)
- **Maersk:** "interactive supply chain dashboard ... powered by AI" with a "consolidated view of shipments, tasks, logistics updates" (marketing copy). The hub homepage has a shipment-number box with "View Details" that goes straight to shipment details. — [Maersk Logistics Hub](https://www.maersk.com/digital-services/logistics-hub); [Maersk FAQ snippets](https://www.maersk.com/support/faqs/where-can-i-find-my-delivery-order-or-container-release-confirmation)
- **CMA CGM:** the dashboard is personalizable ("see only the information that is useful to you"), and data exports can be scheduled daily, weekly or monthly. It has three default views and a To do column. — [CMA CGM shipment dashboard (snippet)](https://www.cma-cgm.com/my-cma-cgm/shipment-dashboard)
- **Hapag-Lloyd Navigator:** Shipments and To Do's tabs, a global search (booking/BL/container/invoice), location/vessel/date filters, column customization and Excel export. — [Navigator 2.0 User Guide PDF](https://www.hapag-lloyd.com/content/dam/website/downloads/pdf/HLAG_Navigator_2.0_User_Guide.pdf)
- **MSC:** the dashboard lets users "manage bookings at a glance" with an overview of all shipments. — [MSC eBusiness](https://www.msc.com/en/ebusiness)
- **Evergreen:** ShipmentLink offers an "All-in-One" entry and an "e-Report" (analytics and notifications) item, and describes itself as "Your smart assistant". — [ShipmentLink](https://www.shipmentlink.com/)
- **ONE:** the Import Shipment Overview dashboard has readiness columns for arrival notice, payment/invoice and manifest/surrender. — [ONE advisory](https://www.one-line.com/en/news/customer-advisory-import-shipment-overview)

### Inferences
- Global search placement: Hapag-Lloyd uses one search box at the top of the dashboard that accepts any reference type (booking/BL/container/invoice). Maersk puts a shipment-number box on the hub home. CMA CGM puts tracking on the home page and in the menu. A single search box that accepts any reference type is the de facto standard.
- How much the dashboards show: carriers keep the home view list-centric with few KPIs. Only the forwarder (Flexport) documents map, analytics and widget-heavy dashboards.

### Gaps
- No quantitative data (widget counts, KPI tiles) was found for carrier dashboards in their 2024–2026 versions.
- Hapag-Lloyd "Hapag-Lloyd Live" is a reefer/container monitoring product (Basic/Plus, hourly data), not a dashboard redesign. No dashboard IA write-up was found for it. — [HL Live Position](https://www.hapag-lloyd.com/en/online-business/track/live-position.html)
- The CMA CGM release notes page (https://www.cma-cgm.com/ECommerce/ReleaseNote) exists but returned 403, so dated release notes could not be extracted.
- No agency case studies or Behance/Dribbble write-ups were found for any of the eight portals in this session (not searched exhaustively due to call budget).
