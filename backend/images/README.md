# Images Folder Structuur

## Overzicht
Deze folder bevat afbeeldingen van auto's, georganiseerd volgens de structuur: `images/{brand}/{model}/{id}.jpg`

## Mapstructuur Voorbeeld
```
images/
├── toyota/
│   ├── camry/
│   │   ├── 123.jpg
│   │   ├── 124.jpg
│   │   └── 125.jpg
│   └── prius/
│       ├── 200.jpg
│       └── 201.jpg
├── ford/
│   ├── focus/
│   │   └── 300.jpg
│   └── mustang/
│       └── 301.jpg
└── mercedes-benz/
    └── e_class/
        └── 400.jpg
```

## Hoe Afbeeldingen Toevoegen

### Optie 1: Handmatig
1. Navigeer naar `images/{brand}/{model}/`
2. Plaats de afbeelding met de naam `{id}.jpg` (waar {id} het ID is van de auto)

### Optie 2: Via een Tool
Een aparte tool kan automatisch afbeeldingen downloaden van legale bronnen en in deze structuur plaatsen.

## Belangrijke Notities

⚠️ **GEEN WEB SCRAPING**: 
- Deze applicatie doet GEEN automatische web scraping
- Alle afbeeldingen moeten legaal verkregen worden
- Zie Program.cs voor uitgebreide uitleg over waarom

✅ **Legale Bronnen**:
- Eigen fotografie
- Licentie-vrije stock foto's (Unsplash, Pexels, etc.)
- Officiële media kits met toestemming
- Commerciële stock services met licentie

## Bestandsformaat
- Extensie: `.jpg` (kan ook `.png` zijn, pas code aan indien nodig)
- Naamgeving: `{auto_id}.jpg` (bijv. `123.jpg` voor auto met ID 123)

## Automatische Pad Generatie
De `AssignImagePaths()` functie in Program.cs genereert automatisch de juiste paden voor alle auto's.
Je hoeft alleen de daadwerkelijke afbeeldingsbestanden in de juiste folders te plaatsen.
