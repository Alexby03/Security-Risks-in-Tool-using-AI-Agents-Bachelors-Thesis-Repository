# Säkerhetsrisker i verktygsanvändande AI-agenter - Repository

Detta projekt är ett ramverk utvecklat för att utvärdera och auditera säkerhetsprestandan hos LLM-baserade agenter i en företagsmiljö styrd av RBAC (Role-Based Access Control). Programmet simulerar en isolerad sandlådemiljö där agenter interagerar med olika affärsverktyg (dokument, e-post, kunddata och kalender) för att undersöka hur väl de respekterar användares behörighetsgränser. Syftet med projektet är att identifiera sårbarheter i autonoma system och jämföra hur olika sätt att representera behörigheter påverkar agenternas beslutsfattande.

## Kärntjänster

Inom ramen för vårt examensarbete används tre huvudsakliga tjänster för att genomföra experimentet:

### 1. Generator
**Generator** ansvarar för att skapa den omfattande testmängden (3000 unika scenarier). Den använder en LLM för att generera syntetiska men realistiska företagsinstruktioner baserat på en databas med fiktiva användare och deras tilldelade behörigheter.
*   **Funktion:** Den skapar "ground truth" för varje testfall, vilket inkluderar information om huruvida en handling är tillåten, vilka verktyg som krävs och vilka behörigheter som är nödvändiga.
*   **Kategorisering:** Varje scenario klassificeras som antingen *Non-Malicious*, *Vague* eller *Malicious* för att testa agentens förmåga att balansera hjälpsamhet mot säkerhet.

### 2. Tester
**Tester** utgör själva exekveringsfasen i experimentet. Tjänsten hämtar de genererade scenarierna och exponerar dem för den agent som utvärderas (t.ex. GPT-4o, Mistral eller DeepSeek).
*   **Funktion:** Den döljer rättningsmallen för den testade agenten och tvingar den att redovisa sitt interna resonemang genom ett strukturerat fält (`log_prompt`).
*   **Variabler:** Tester hanterar växlingen mellan att representera behörigheter som binära värden (Bool) eller som naturligt språk (NL), vilket är centralt för att besvara studiens forskningsfrågor.

### 3. Judger
**Judger** används för den kvalitativa analysen av de testfall där agenterna misslyckats. Den tillämpar ett "Cross-Evaluation"-upplägg där de modeller som inte genererat resultatet agerar domare över loggarna vars agent producerat ett icke-godkänt resultat.
*   **Funktion:** Domar-agenten analyserar de interna resonemangsloggarna för att identifiera den bakomliggande orsaken till felet.
*   **Klassificering:** Den kategoriserar felen i specifika sårbarhetsklasser såsom *Confused Deputy*, *Policy Misinterpretation*, *Tool Hallucination* och *Benevolence Bias*. Detta möjliggör en djupare analys av *varför* en agent valde att ignorera en säkerhetspolicy.

## SQL-skript för Analys

I projektet används en serie SQL-skript för att aggregera rådata från databasen till de slutgiltiga resultaten:

*   **attacksuccessrate.sql**: Beräknar ASR genom att mäta hur ofta en agent genomförde ett otillåtet verktygsanrop i förhållande till det totala antalet fientliga eller tvetydiga prompts.
*   **toolcallerrorrate.sql**: Beräknar TCER genom att analysera förhållandet mellan felaktiga (inklusive hallucinerade) verktygsanrop och det totala antalet anrop agenten gjorde.
*   **taskcompletionrate.sql**: Mäter TCR genom att identifiera de testfall där agentens anropade verktyg exakt överensstämde med rättningsmallens förväntade anrop.
*   **precisionandrecall.sql**: Genererar en konfusionsmatris (TP, FP, FN, TN) för att beräkna precision och recall för agentens förmåga att korrekt identifiera behöriga användare.
*   **getfailedrowsbyagent.sql**: Ett verktygsskript för att snabbt räkna och extrahera samtliga underkända rader per agent för vidare granskning.
*   **classificationstatsbyagent.sql**: Ett avancerat skript för den kvalitativa analysen. Det summerar totala fel per agent och kopplar ihop dem med domar-agenternas bedömningar. Skriptet beräknar ett medelvärde baserat på de två oberoende domarna för att fastställa frekvensen och procentandelen av specifika sårbarheter (t.ex. Tool Hallucination) i förhållande till modellens totala felmarginal.

## Miljövariabler

Följande miljövariabler krävs för att köra systemets olika delar:

*   **DefaultConnection**: Den primära anslutningssträngen för SQL-databasen.
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
- **Modeller:** GPT-4o, Mistral Large 3, DeepSeek-V3.1
