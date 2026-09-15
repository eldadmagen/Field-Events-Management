# ארכיטקטורה והחלטות עיצוב - Field Events Management

מסמך זה עונה במפורש על השאלות שהוגדרו בדרישות הפרויקט, ומסביר את הבחירות שנעשו והחלופות שנשקלו.

## 1. ה-Agent

### ארכיטקטורת המימוש: .NET Worker Service (Generic Host + BackgroundService)

ה-Agent (`src/FieldEvents.Agent`) בנוי כתהליך `Microsoft.NET.Sdk.Web` המריץ שני דברים באותו host:
1. Minimal API (Kestrel) עם endpoint גנרי `POST /ingest/{sourceId}` שחושף את ה-Agent למקורות חיצוניים.
2. `BackgroundService` (`OutboxForwarder`) שמחזיק את חיבור ה-SignalR לשרת ומרוקן את ה-outbox המקומי ברקע.

**חלופות שנשקלו:**

| חלופה | הכרעה | נימוק |
|---|---|---|
| **Worker Service / Generic Host** (נבחר) | ✅ | תהליך רקע רציף, cross-platform (`UseWindowsService()`/`UseSystemd()` זמינים בלי לשנות קוד), DI/Config/Logging מובנים, יכול להריץ כמה background tasks במקביל (HTTP listener + drain loop) תחת host אחד. מתאים בול לדרישה "רץ ברציפות ברקע". |
| Azure Functions / סביבה serverless | ❌ נדחה | מתאים לטריגרים קצרי-חיים; לא מתאים לחיבור SignalR מתמשך (stateful) ולא ל-state מקומי (outbox) שצריך לשרוד בין הפעלות. היה דורש ארכיטקטורה שונה לגמרי. |
| Windows Service קלאסי (`System.ServiceProcess`) | ❌ נדחה | מודל ישן יותר. Worker Service הוא ה"יורש" המודרני שלו מעל Generic Host - אותה יכולת פריסה כ-Windows Service, אך עם ארכיטקטורה ניתנת לתחזוקה/הרחבה טוב יותר. |

### חשיפה למקורות חיצוניים

`POST /ingest/{sourceId}` מקבל JSON בפורמט אחיד (`IncomingEventRequest`: כותרת, תיאור, מיקום, עדיפות). כל מקור מזוהה ב-URL (`sourceId`) ומאומת מול `X-Api-Key` (ראו "אימות מקורות" למטה).

### מנגנון תקשורת Agent ↔ Server: SignalR (WebSocket) client מתמיד

`OutboxForwarder` מחזיק `HubConnection` (חבילת `Microsoft.AspNetCore.SignalR.Client`) עם `WithAutomaticReconnect()` מול `EventsHub` בשרת, וקורא ל-`ReportEvent` על כל אירוע.

**יתרונות:** דו-כיווני (מאפשר בעתיד לשרת "לדבר" בחזרה ל-Agent, למשל לשלוח פקודות), reconnect מובנה, over TLS, latency נמוך - אין overhead של HTTP handshake לכל הודעה כי החיבור כבר פתוח. מתאים בדיוק לדרישה "הודעות קצרות בזמן אמת".

**חסרונות / גבולות אחריות:** מצריך ניהול חיבור stateful. האחריות על *guaranteed delivery* היא של ה-Agent (outbox), לא של ה-Hub - ה-Hub רק מאשר קבלה (ack) אחרי ששמר ל-DB.

**חלופה שנשקלה ונדחתה:** קריאות REST רגילות (`HttpClient.PostAsync`) מה-Agent לשרת, עם retry (למשל דרך Polly). פשוטה יותר ו-stateless, אך חד-כיוונית וכל אירוע = round-trip HTTP נפרד - overhead גבוה יותר לעומת חיבור פתוח, ופחות "בזמן אמת".

### אימות מקורות (Agent)

כל מקור רשום ב-config (`src/FieldEvents.Agent/appsettings.json`, מקטע `Sources`) עם `Id` + `ApiKey`. `SourceRegistry.Validate(sourceId, apiKey)` בודק את הכותרת `X-Api-Key` מול הרשימה לפני שהאירוע נכנס ל-outbox. מקור לא מאומת מקבל `401`.

### מה קורה כשהשרת המרכזי לא זמין

זו הסיבה המרכזית לקיומו של ה-**outbox מקומי** (`OutboxStore`, טבלת SQLite נפרדת בתוך ה-Agent, `agent-outbox.db`):

1. כל אירוע שמתקבל מ-`/ingest` נכתב קודם ל-outbox, ורק אז ה-endpoint מחזיר תשובה למקור (202 Accepted) - המקור מקבל אישור מהיר גם אם השרת המרכזי כרגע לא זמין.
2. `OutboxForwarder` מנסה לשלוח כל שורה בתור, ומוחק אותה מה-outbox **רק** אחרי ack מהשרת.
3. אם החיבור נופל - `WithAutomaticReconnect()` מנסה מחדש עם backoff; אם הניסיונות האוטומטיים נגמרים, הלולאה הראשית של ה-`BackgroundService` ממשיכה לנסות `StartAsync` כל 2 שניות ללא הגבלת זמן.
4. מכיוון שה-outbox נשמר על דיסק (לא רק בזיכרון), גם אם ה-Agent עצמו קורס/מופעל מחדש באמצע ההפסקה - האירועים שלא אושרו עדיין קיימים ונשלחים כשהחיבור חוזר.
5. `OutboxId` (GUID שנוצר ב-Agent) הוא מפתח אידמפוטנטיות: אם ה-Agent שולח שוב אירוע שכבר נשלח בעבר (למשל כי הוא לא ראה את ה-ack), `EventsHub.ReportEvent` מזהה את הכפילות ומחזיר את אותו `ServerEventId` בלי ליצור רשומה כפולה.

### הוספת מקור חדש

הוספת מקור = שורה חדשה במקטע `Sources` ב-config (`Id` + `ApiKey`) - אין צורך בשינוי קוד, כל עוד המקור שולח JSON בפורמט האחיד (`IncomingEventRequest`). מקור עם פורמט payload שונה לחלוטין ידרוש שכבת מיפוי קטנה (adapter) לפני קריאה ל-`OutboxStore.EnqueueAsync` - אבל צנרת ה-outbox/forwarding/retry נשארת זהה.

## 2. השרת המרכזי (Backend)

### State Machine

`EventStateMachine` (`src/FieldEvents.Server/Domain/EventStateMachine.cs`) הוא class סטטי, ללא תלויות, עם מפת מעברים חוקיים מפורשת:

```
New        -> Assigned, Cancelled
Assigned   -> InProgress, Cancelled
InProgress -> Assigned (העברה בין טכנאים), Completed, Cancelled
Completed  -> (מצב סופי)
Cancelled  -> (מצב סופי)
```

`FieldEvent.TransitionTo(...)` הוא הדרך **היחידה** לשנות סטטוס (ה-setter של `Status` הוא `private`) - כל שינוי עובר דרך המפה, ומוסיף רשומת `EventStatusHistory` (סטטוס קודם, סטטוס חדש, מי ביצע, מתי). מעבר לא חוקי זורק `InvalidEventTransitionException`. מכוסה במלואו ב-`tests/FieldEvents.Server.Tests` (20 בדיקות: כל מעבר חוקי, כל מעבר לא חוקי, ושרשור מעברים).

### הרשאות

JWT עם claim מסוג `role` (`Dispatcher`/`Technician`), נאכף אך ורק בצד השרת עם `[Authorize(Roles = ...)]` על ה-Controllers (למשל `GET /api/events` מוגבל ל-Dispatcher, `GET /api/events/mine` ל-Technician) - לעולם לא באמון על ה-UI. נבדק ידנית: טכנאי שמנסה לגשת ל-endpoint של סדרן מקבל `403`.

### Real-time לקוחות - שני מסלולים

**משתמש מחובר:** `ClientsHub` (SignalR). `ConnectionManager` בזיכרון עוקב אחרי אילו משתמשים מחוברים כרגע (`OnConnectedAsync`/`OnDisconnectedAsync`), ומוסיף כל חיבור לקבוצות לפי role (`Dispatchers`/`Technicians`) ולקבוצה אישית (`user-{id}`). זה מה ש"מלמד את השרת" באיזה מצב המשתמש נמצא.

**משתמש מנותק:** תוכנן כ-Web Push (VAPID) + Service Worker בצד הלקוח, אך **לא מומש בפועל** - זהו stub מכוון (`IPushNotificationChannel` / `WebPushNotificationChannel`), עם תיעוד מלא של מה חסר בקובץ עצמו. טבלת `PushSubscription` קיימת ב-DB לצורך זה.

`NotificationService` הוא המקום שבו קורה ה"מעבר בין שני המצבים": `NotifyTechnicianAsync` בודק ב-`ConnectionManager.IsOnline(userId)` - אם המשתמש מחובר שולח ב-SignalR, אחרת קורא ל-`IPushNotificationChannel` (stub). ה-Flow הנדרש (התראת סדרן על אירוע חדש) עובר תמיד דרך `ClientsHub` ל-group `Dispatchers`, ולכן ממומש E2E באופן מלא.

### אבטחה

- **משתמשי קצה:** JWT Bearer (נבחר על פני Cookie/Session בגלל SPA + SignalR; הטוקן מועבר גם כ-query string ב-handshake של SignalR - תבנית סטנדרטית כי דפדפנים לא יכולים להוסיף headers מותאמים אישית ל-WebSocket handshake).
- **Agent -> Server:** scheme נפרד (`AgentApiKeyAuthenticationHandler`) - מפתח קבוע ב-config, שונה לחלוטין ממנגנון ה-JWT של משתמשי הקצה, כדי להפריד בבירור בין "משתמש אנושי מחובר" ל"שירות פנימי מהימן".
- **הצפנה:** HTTPS/WSS בכל הערוצים (Kestrel + HTTPS redirection; בפיתוח מקומי - `dotnet dev-certs https --trust`).

## 3. Skeleton מכוון (לא ממומש E2E)

לפי דרישות הפרויקט, רק Flow אחד צריך מימוש E2E מלא. הדברים הבאים קיימים ברמת מבנה/interface אך אינם ממומשים במלואם:

- **Web Push אמיתי** (`WebPushNotificationChannel`) - רק לוג "היה נשלח push".
- **Technician UI מחובר ל-SignalR בזמן אמת** - כרגע קורא REST בלבד (`GET /api/events/mine`); הקריאה לצד-שרת (`NotifyTechnicianAsync`) כבר קיימת ועובדת, רק אין מאזין בצד ה-Angular עבור טכנאי.
- הקצאה/העברה/הערות (`EventsController`) **כן** ממומשות בפועל (לא stub) כי הן זולות לממש ברגע שיש State Machine ו-`INotificationService`, אבל אינן חלק מה-Flow הנדרש ולא קיבלו את אותה רמת בדיקות.

## סיכום Trade-offs מרכזיים

| החלטה | המחיר ששילמנו | מה קיבלנו בתמורה |
|---|---|---|
| SignalR ולא REST פשוט בין Agent ל-Server | מורכבות ניהול חיבור stateful | Real-time אמיתי, ערוץ דו-כיווני עתידי |
| SQLite ולא SQL Server | פחות מתאים לעומס/concurrency גבוה | אפס התקנה, "clone and run" מיידי - מתאים לנפח הנתונים הקטן שתואר בדרישות |
| Outbox מקומי ב-Agent (SQLite נפרד) | טבלה/קוד נוסף לתחזק | אירוע לעולם לא הולך לאיבוד, גם בקריסת Agent |
| Web Push כ-stub בלבד | המסלול "משתמש מנותק" לא עובד בפועל | התמקדות מלאה באיכות ה-Flow הנדרש, בהתאם לדרישות הפרויקט |
