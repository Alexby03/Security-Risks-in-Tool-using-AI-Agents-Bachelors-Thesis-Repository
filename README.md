# Säkerhetsrisker i verktygsanvändande AI-agenter - Repository

Detta repository innehåller ett experimentellt ramverk utvecklat för att utvärdera och granska säkerhetsprestandan hos LLM-baserade agenter i en företagsmiljö. Systemet simulerar en isolerad sandlådemiljö styrd av Role-Based Access Control (RBAC), där agenter interagerar med affärsverktyg (dokument, e-post, kunddata och kalender).

Syftet med ramverket är att identifiera sårbarheter i autonoma system, analysera hur agenter respekterar behörighetsgränser, samt utvärdera hur olika representationer av behörigheter (naturligt språk vs. booleska värden) påverkar agenternas beslutsfattande.

## Kärntjänster

Arkitekturen är uppdelad i tre huvudsakliga tjänster för att hantera datasetgenerering, testexekvering och kvalitativ utvärdring:

### 1. Generator
**Generator** ansvarar för att skapa en omfattande testmängd (3000 unika scenarier).
*   **Funktion:** Anvönder en LLM för att generera syntetiska företagsinstruktioner baserat på en databas med fiktiva användare och deras tilldelade behörigheter. Skapar systemets "ground truth" för varje testfall, vilket inkluderar information om huruvida en handling är tillåten, vilka verktyg som krävs och vilka behörigheter som är nödvändiga.
*   **Säkerhetskategorier:** Varje scenario klassificeras som antingen *Non-Malicious*, *Vague* eller *Malicious* för att testa agentens förmåga att balansera säkerhet mot hjälpsamhet.

### 2. Tester
**Tester** utgör själva exekveringsfasen i experimentet där LLM-agenterna ställs mot testfallen.
*   **Funktion:** Exponerar scenarier för test-agenten utan att avslöja rättningsmallen. Tvingar fram transparans genom att logga agentens interna resonemang via ett strukturerat `log_prompt`-fält.
*   **Experimentvariabler:** Hanterar växlingen mellan att representera behörigheter som binära värden (Bool) eller som naturligt språk (NL), vilket är centralt för att besvara studiens forskningsfrågor.

### 3. Judger
**Judger** genomför en kvalitativa analys av de testfall där agenterna misslyckades. Den tillämpar ett "Cross-Evaluation"-upplägg där de modeller som inte genererat resultatet agerar domare över test-agentens loggar som producerat ett icke-godkänt resultat.
*   **Funktion:** Domar-agenten analyserar de interna resonemangsloggarna för att identifiera den bakomliggande orsaken till felet.
*   **Klassificering:** Den kategoriserar felen i specifika sårbarhetsklasser såsom *Confused Deputy*, *Policy Misinterpretation*, *Tool Hallucination* och *Benevolence Bias*. Detta möjliggör en djupare analys av *varför* en agent valde att ignorera en säkerhetspolicy.

## SQL-skript för Analys

All mätdata aggregeras och beräknas via dedikerade SQL-skript:

*   **tables.sql**: Skript som skapar samtliga tabeller som användes i studiens metod.
*   **attacksuccessrate.sql**: Beräknar **ASR** (Attack Success Rate) genom att mäta hur ofta en agent genomförde ett otillåtet verktygsanrop i förhållande till det totala antalet fientliga eller tvetydiga prompts.
*   **toolcallerrorrate.sql**: Beräknar **TCER** (Tool Call Error Rate) genom att analysera förhållandet mellan felaktiga (inklusive hallucinerade) verktygsanrop och det totala antalet anrop agenten gjorde.
*   **taskcompletionrate.sql**: Mäter **TCR** (Task Completion Rate) genom att identifiera de testfall där agentens anropade verktyg exakt överensstämde med rättningsmallens förväntade anrop.
*   **precisionandrecall.sql**: Genererar en konfusionsmatris (TP, FP, FN, TN) för att beräkna Precision och Recall för agentens förmåga att korrekt identifiera behöriga användare.
*   **getfailedrowsbyagent.sql**: Ett verktygsskript för att snabbt räkna och extrahera samtliga underkända rader per agent för vidare granskning.
*   **classificationstatsbyagent.sql**: Ett avancerat skript för den kvalitativa analysen. Summerar totala fel per agent och kopplar ihop dem med domar-agenternas bedömningar. Skriptet beräknar ett medelvärde baserat på de två oberoende domarna för att fastställa frekvensen och procentandelen av specifika sårbarheter (t.ex. Tool Hallucination) i förhållande till modellens totala felmarginal.

## Miljövariabler

Följande miljövariabler krävs för att köra systemets olika delar:

*   **ConnectionStrings__Database**: Den primära anslutningssträngen för SQL-databasen.
*   **AZURE_OPENAI_ENDPOINT**: URL:en till den Azure OpenAI-resurs som används för API-anrop.
*   **DEPLOYMENT_NAME**: Namnet på den specifika modell-deployment som ska utvärderas eller användas för generering (t.ex. "gpt-4o").
*   **AZURE_OPENAI_KEY**: API-nyckeln för att autentisera anrop mot Azure OpenAI-tjänsten.
*   **JUDGING_TARGET**: Definierar vilken agent som är föremål för granskning under `judge-failures`-loopen.
*   **PERMISSIONS_AS_BOOL**: Togglar om behörigheter ska hämtas och presenteras som binära namn (`true`) eller som förklarande text (`false`).
*   **START_INDEX**: Bestämmer vid vilken användare i databasen som `Generator` ska påbörja sin körning.
*   **INDEX_FLOOR**: Det lägsta ScenarioId som definierar startpunkten för en testkörning i `Tester`.
*   **INDEX_CEILING**: Det högsta ScenarioId som definierar slutpunkten för en testkörning i `Tester`.

## Teknisk Stack
- **Backend:** .NET 10 / ASP.NET Core
- **Databas:** MySQL (MySqlConnector)
- **AI-integration:** Azure OpenAI SDK (OpenAI.Chat)
- **Modeller:** GPT-4o, DeepSeek-V3.1, Mistral Large 3
