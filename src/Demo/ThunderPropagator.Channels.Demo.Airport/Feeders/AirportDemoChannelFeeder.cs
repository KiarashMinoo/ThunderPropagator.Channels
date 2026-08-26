using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Bogus;
using ThunderPropagator.Application.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Channel;
using ThunderPropagator.Channels.Demo.Airport.Feeders;
using ThunderPropagator.Channels.Demo.Airport.Messages;
using ThunderPropagator.Channels.Demo.Airport.Metadata;

namespace ThunderPropagator.Channels.Demo.Airport.Feeders
{
    internal
#if !DEBUG
        sealed
#endif
        class AirportDemoChannelFeeder : IterativeFeeder<AirportDemoChannel, AirportDemoChannelFeederMessage, AirportDemoChannelFeederConfiguration>
    {
        private static readonly string[] Airports =
        [
            "Adelaide (ADL)",
            "Brisbane (BNE)",
            "Cairns (CNS)",
            "Darwin (DRW)",
            "Gold Coast (OOL)",
            "Melbourne (MEL)",
            "Perth (PER)",
            "Sydney (SYD)",
            "Dhaka (DAC)",
            "Phnom Penh (PNH)",
            "Ahmedabad (AMD)",
            "Bangalore (BLR)",
            "Calicut (CCJ)",
            "Chennai (MAA)",
            "Delhi (DEL)",
            "Goa (GOX)",
            "Hyderabad (HYD)",
            "Jaipur (JAI)",
            "Kolkata (CCU)",
            "Mumbai (BOM)",
            "Trivandrum (TRV)",
            "Denpasar (DPS)",
            "Jakarta (CGK)",
            "Chubu (NGO)",
            "Fukuoka (FUK)",
            "Haneda (HND)",
            "New Chitose Airport (CTS)",
            "Osaka-Kansai (KIX)",
            "Tokyo-Narita (NRT)",
            "Kota Kinabalu (BKI)",
            "Kuala Lumpur (KUL)",
            "Kuchung (KCH)",
            "Langkawi (LGK)",
            "Penang (PEN)",
            "Senai (JHB)",
            "Auckland (AKL)",
            "Christchurch (CHC)",
            "Queenstown (ZQN)",
            "Wellington (WLG)",
            "Islamabad (ISB)",
            "Karachi (KHI)",
            "Lahore (LHE)",
            "Peshawar (PEW)",
            "Port Moresby (POM)",
            "Manila (MNL)",
            "Incheon (ICN)",
            "Jeju International Airport (CJU)",
            "Seoul (GMP)",
            "Male (MLE)",
            "Changi (SIN)",
            "Colombo (CMB)",
            "Chiang Mai (CNX)",
            "Chiang Rai International Airport (CEI)",
            "Don Mueang (DMK)",
            "Hat Yai (HDY)",
            "Krabi (KBV)",
            "Phuket (HKT)",
            "Ko Samui  (USM)",
            "Suvarnabhumi (BKK)",
            "Utapao (UTP)",
            "Hanoi (HAN)",
            "Ho Chi Minh (SGN)",
            "Graz (GRZ)",
            "Innsbruck (INN)",
            "Klagenfurt (KLU)",
            "Linz (LNZ)",
            "Salzburg (SZG)",
            "Vienna (VIE)",
            "Brussels (BRU)",
            "Brussels South Charleroi (CRL)",
            "Sofia (SOF)",
            "Dubrovnik (DBV)",
            "Pula (PUY)",
            "Split (SPU)",
            "Zadar (ZAD)",
            "Zagreb International Airport (ZAG)",
            "Larnaca (LCA)",
            "Paphos (PFO)",
            "Prague (PRG)",
            "Billund (BLL)",
            "Copenhagen (CPH)",
            "Vágar (FAE)",
            "Helsinki (HEL)",
            "Kittilä (KTT)",
            "Rovaniemi (RVN)",
            "Annecy (NCY)",
            "Basel-Mulhouse-Freiburg (BSL/MLH)",
            "Chambery (CMF)",
            "Figari (FSC)",
            "Lyon (LYS)",
            "Nantes (NTE)",
            "Nice (NCE)",
            "Paris Beauvais-Tille Airport (BVA)",
            "Paris-Ch. De Gaulle (CDG)",
            "Paris-Orly (ORY)",
            "Berlin - Schonefeld (BER)",
            "Bremen (BRE)",
            "Cologne (CGN)",
            "Dresden (DRS)",
            "Duesseldorf (DUS)",
            "Erfut (ERF)",
            "Frankfurt (FRA)",
            "Hamburg (HAM)",
            "Hannover (HAJ)",
            "Leipzig (LEJ)",
            "Memmingen (FMM)",
            "Muenster (FMO)",
            "Munich (MUC)",
            "Nuremberg (NUE)",
            "Saarbruecken (SCN)",
            "Stuttgart (STR)",
            "Aktion (PVK)",
            "Athens (ATH)",
            "Chania (CHQ)",
            "Chios (JKH)",
            "Corfu (CFU)",
            "Heraklion (HER)",
            "Kalamata (KLX)",
            "Karpathos (AOK)",
            "Kefallinia (EFL)",
            "Kithira (KIT)",
            "Kos (KGS)",
            "Lemnos (LXS)",
            "Mikonos (JMK)",
            "Paros (PAS)",
            "Patras (GPA)",
            "Rhodes (RHO)",
            "Siteia (JSH)",
            "Skiathos (JSI)",
            "Thessalonika (SKG)",
            "Thira (JTR)",
            "Volos (VOL)",
            "Zakinthos (ZTH)",
            "Ilulissat Airport (JAV)",
            "Kangerlussuaq Airport (SFJ)",
            "Nuuk Airport (GOH)",
            "Budapest (BUD)",
            "Akureyri (AEY)",
            "Egilsstadir (EGS)",
            "Keflavik (KEF)",
            "Dublin (DUB)",
            "Eilat (ETM)",
            "Tel Aviv (TLV)",
            "Alghero (AHO)",
            "Bari (BRI)",
            "Bologna (BLQ)",
            "Brindisi (BDS)",
            "Cagliari (CAG)",
            "Catania (CTA)",
            "Florence (FLR)",
            "Genova Cristoforo Colombo (GOA)",
            "Lamezia Terme (SUF)",
            "Lampedusa (LMP)",
            "Milan - Linate (LIN)",
            "Milan - Malpensa (MXP)",
            "Milan - Orio al Serio (BGY)",
            "Naples (NAP)",
            "Olbia Costa Smeralda (OLB)",
            "Palermo (PMO)",
            "Pantelleria (PNL)",
            "Pisa (PSA)",
            "Rimini Federico Fellini (RMI)",
            "Rome - Ciampino (CIA)",
            "Rome - Fiumicino (FCO)",
            "Treviso (TSF)",
            "Turin (TRN)",
            "Venice (VCE)",
            "Verona (VRN)",
            "Jersey (JER)",
            "Pristina (PRN)",
            "Riga (RIX)",
            "Vilnius (VNO)",
            "Luxembourg (LUX)",
            "Luqa (MLA)",
            "Amsterdam (AMS)",
            "Eindhoven (EIN)",
            "Rotterdam (RTM)",
            "Skopje (SKP)",
            "Ålesund (AES)",
            "Alta (ALF)",
            "Bergen (BGO)",
            "Bodø (BOO)",
            "Harstad-Narvik (EVE)",
            "Haugesund (HAU)",
            "Kirkenes (KKN)",
            "Kristiansund (KSU)",
            "Kristiandsand (KRS)",
            "Molde (MOL)",
            "Oslo-Gardermoen (OSL)",
            "Stavanger (SVG)",
            "Svalbard (LYR)",
            "Torp (TRF)",
            "Tromsø (TOS)",
            "Trondheim (TRD)",
            "Krakow (KRK)",
            "Poznan   (POZ)",
            "Warsaw Chopina (WAW)",
            "Faro (FAO)",
            "Funchal (FNC)",
            "Lisbon (LIS)",
            "Ponta Delgada (PDL)",
            "Porto (OPO)",
            "Moscow-Domodedovo (DME)",
            "Moscow-Sheremetyevo (SVO)",
            "Moscow-Vnukovo (VKO)",
            "St. Petersburg (LED)",
            "Bratislava (BTS)",
            "Ljubljana (LJU)",
            "Alicante (ALC)",
            "Almeria (LEI)",
            "Asturias (OVD)",
            "Barcelona (BCN)",
            "Bilbao (BIO)",
            "Corvera (RMU)",
            "Fuerteventura (FUE)",
            "Gerona (GRO)",
            "Gran Canaria (LPA)",
            "Granada (GRX)",
            "Ibiza (IBZ)",
            "Jerez (XRY)",
            "La Coruna (LCG)",
            "La Palma (SPC)",
            "Lanzarote (ACE)",
            "Madrid Barajas (MAD)",
            "Malaga (AGP)",
            "Menorca (MAH)",
            "Palma Mallorca (PMI)",
            "Pamplona (PNA)",
            "Reus (REU)",
            "San Sebastian (EAS)",
            "Santander (SDR)",
            "Seville (SVQ)",
            "Tenerife - Norte (TFN)",
            "Tenerife - Sur (TFS)",
            "Valencia (VLC)",
            "Ängelholm Helsingborg (AGH)",
            "Åre Östersund (OSD)",
            "Arvidsjaur (AJR)",
            "Gothenburg (GOT)",
            "Kiruna (KRN)",
            "Ronneby Airport (RNB)",
            "Skellefteå (SFT)",
            "Stockholm-Arlanda (ARN)",
            "Stockholm-Bromma (BMA)",
            "Visby Airport Swedavia (VBY)",
            "Geneva (GVA)",
            "Zurich (ZRH)",
            "Ankara (ESB)",
            "Antalya (AYT)",
            "Ataturk (ISL)",
            "Bodrum (BJV)",
            "Dalaman (DLM)",
            "Istanbul (IST)",
            "Istanbul Sabiha Gökçen (SAW)",
            "Izmir (ADB)",
            "Kyiv (KBP)",
            "Zhuliany (IEV)",
            "Aberdeen (ABZ)",
            "Belfast- Belfast City (BHD)",
            "Belfast- Belfast International (BFS)",
            "Birmingham (BHX)",
            "Bristol (BRS)",
            "East Midlands (EMA)",
            "Edinburgh (EDI)",
            "Glasgow (GLA)",
            "Leeds Bradford (LBA)",
            "Liverpool (LPL)",
            "London-City (LCY)",
            "London-Gatwick (LGW)",
            "London-Heathrow (LHR)",
            "London-Luton (LTN)",
            "Manchester (MAN)",
            "Newcastle (NCL)",
            "Southampton (SOU)",
            "Stansted (STN)",
            "Bahrain (BAH)",
            "Borg El Arab (HBE)",
            "Cairo (CAI)",
            "Hurghada (HRG )",
            "Marsa Alam (RMF)",
            "Sharm El Sheikh (SSH)",
            "Sohag (HMB)",
            "Accra (ACC)",
            "Kuwait (KWI)",
            "Casablanca (CMN)",
            "Marrakech (RAK)",
            "Rabat Salé (RBA)",
            "Doha (DIA)",
            "Doha (DOH)",
            "Aeroporto Internacional Amilcar Cabral (SID)",
            "Aeroporto Internacional Aristides Pereira (BVC)",
            "Aeroporto Internacional Nelson Mandela (RAI)",
            "Abha (AHB)",
            "Al-Ahsa (HOF)",
            "Arar (RAE)",
            "Buraydah (ELQ)",
            "Dammam (DMM)",
            "Ha'il (HAS)",
            "Jeddah (JED)",
            "Jizan (GIZ)",
            "Medina (MED)",
            "Neom Bay (NUM)",
            "Riyadh (RUH)",
            "Sakakah (AJF)",
            "Tabuk (TUU)",
            "Ta'if (TIF)",
            "Yanbu (YNB)",
            "Seychelles (SEZ)",
            "Cape Town (CPT)",
            "Durban (DUR)",
            "Johannesburg (JNB)",
            "Muscat International (MCT)",
            "Monastir (MIR)",
            "Tunis-Carthage (TUN)",
            "Abu Dhabi (AUH)",
            "Dubai Al Maktoum International (DWC)",
            "Dubai International (DXB)",
            "Beijing Capital (PEK)",
            "Beijing Daxing (PKX)",
            "Changchun (CGQ)",
            "Changsha Huanghua (CSX)",
            "Chengdu Shuangliu (CTU)",
            "Chengdu Tianfu (TFU)",
            "Chongqing Jiangbei (CKG)",
            "Dalian Zhoushuizi (DLC)",
            "Fuzhou Changle (FOC)",
            "Guangzhou Baiyun (CAN)",
            "Guiyang (KWE)",
            "Haikou Meilan (HAK)",
            "Hangzhou Xiaoshan (HGH)",
            "Harbin (HRB)",
            "Hohhot (HET)",
            "Hulun Buir (HLD)",
            "Jinan (TNA)",
            "Kunming Changshui (KMG)",
            "Lanzhou (LHW)",
            "Nanchang (KHN)",
            "Nanjing Lukou (NKG)",
            "Nanning (NNG)",
            "Qingdao Liuting (TAO)",
            "Sanya Fenghuang (SYX)",
            "Shanghai Hongqiao (SHA)",
            "Shanghai Pudong (PVG)",
            "Shenyang (SHE)",
            "Shenzhen Bao'an (SZX)",
            "Shijiazhuang (SJW)",
            "Tianjin Binhai (TSN)",
            "Taiyuan (TYN)",
            "Urumchi Diwopu (URC)",
            "Wuhan Tianhe (WUH)",
            "Xi’an Xianyang (XIY)",
            "Xiamen Gaoqi (XMN)",
            "Zhengzhou (CGO)",
            "Zhuhai (ZUH)",
            "Kaohsiung (KHH)",
            "Taipei (TPE)",
            "Hong Kong (HKG)",
            "Macau (MFM)",
            "Bermuda (BDA)",
            "Aeroporto Internacional De Brasília (BSB)",
            "Aeroporto Internacional De Campinas - Viracopos (VCP)",
            "Aeroporto Internacional Tancredo Neves (CNF)",
            "Florianopolis (FLN)",
            "Fortaleza (FOR)",
            "Porto Alegre (POA)",
            "Recife (REC)",
            "Rio de Janeiro (GIG)",
            "Rio de Janeiro (SDU)",
            "Salvador (SSA)",
            "Sao Paulo (GRU)",
            "Sao Paulo (CGH)",
            "Pampulha (PLU)",
            "Calgary (YYC)",
            "Montreal (YUL)",
            "Quebec (YQB)",
            "Toronto (YTZ)",
            "Toronto (YYZ)",
            "Vancouver (YVR)",
            "Grand Cayman (GCM)",
            "Bogota (BOG)",
            "Havana (HAV)",
            "Holguin (HOG)",
            "Santa Clara (SNU)",
            "Varadero (VRA)",
            "Santo Domingo (SDQ)",
            "Mexico City (MEX)",
            "Lima (LIM)",
            "Providenciales International Airport (PLS)",
            "Chicago-O' Hare (ORD)",
            "Los Angeles (LAX)",
            "New York-J.F. Kennedy (JFK)",
            "Newark (EWR)",
            "Orlando (MCO)",
            "San Francisco (SFO)",
            "Seattle-Tacoma International Airport (SEA)"
        ];

        [SuppressMessage("ReSharper", "MemberInitializerValueIgnored")]
        private readonly HashSet<AirportDemoChannelFeederMessage> _flights = [];

        // Tracks active subscriptions locally via the channel's public SubscriptionAdded/Removed
        // events, since neither is exposed to feeder code any other way. Read with Volatile.Read
        // and written with Interlocked so the poll loop always sees the latest count.
        private int _activeSubscriptions;

        private readonly AirportDemoChannelFeederConfiguration _feederConfiguration;

        public AirportDemoChannelFeeder(AirportDemoChannel channel,
            AirportDemoChannelFeederConfiguration feederConfiguration,
            IFeederHandler<AirportDemoChannel, AirportDemoChannelFeederMessage> feederHandler,
            IServiceProvider serviceProvider)
            : base(channel, feederConfiguration, feederHandler, serviceProvider)
        {
            _feederConfiguration = feederConfiguration;
            _flights = GenerateAirports(2);

            channel.SubscriptionAdded += (_, _) => Interlocked.Increment(ref _activeSubscriptions);
            channel.SubscriptionRemoved += (_, _) => Interlocked.Decrement(ref _activeSubscriptions);
        }

        private HashSet<AirportDemoChannelFeederMessage> GenerateAirports(int maxHours = 1, int terminalDeparturesPerHour = 4)
        {
            var lastFlight = _flights.MaxBy(flight => flight.Departure);
            var minDeparture = lastFlight?.Departure ?? DateTime.UtcNow.TimeOfDay;
            var minuteFlag = 60 / terminalDeparturesPerHour;
            var terminalGenerationCount = (Random.Shared.Next(32, Airports.Length) / 8) + (maxHours * terminalDeparturesPerHour);

            return Enumerable.Range(1, 8)
                .SelectMany(terminal => new Faker<AirportDemoChannelFeederMessage>()
                    .RuleFor(x => x.Key, AirportDemoChannelMetadata.AirportDemo)
                    .RuleFor(x => x.Destination, f => f.PickRandom(Airports))
                    .RuleFor(x => x.Airline, $"({nameof(ThunderPropagator)} Airlines) RSAL")
                    .RuleFor(x => x.Flight, f => $"RS-{f.Random.Int(1000, 9999)}")
                    .RuleFor(x => x.Terminal, terminal)
                    .RuleFor(x => x.Status, Statuses.ScheduledOnTime)
                    .RuleFor(x => x.Departure, f =>
                    {
                        var min = f.IndexFaker * minuteFlag;
                        var max = min + minuteFlag;
                        var minutesToAdd = Random.Shared.Next(min, max);
                        return minDeparture.Add(TimeSpan.FromMinutes(minutesToAdd));
                    })
                    .Generate(terminalGenerationCount)
                    .ToList())
                .ToHashSet();
        }

        protected override async IAsyncEnumerable<FeederReceivedMessage<AirportDemoChannelFeederMessage>> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(_feederConfiguration.PollInterval, cancellationToken);

            if (Volatile.Read(ref _activeSubscriptions) <= 0)
                yield break;

            var flightsToRemove = _flights
                .Where(airport => airport.Departure < DateTime.UtcNow.AddHours(-1).TimeOfDay)
                .ToArray();

            if (flightsToRemove.Length > 0)
            {
                foreach (var flightToRemove in flightsToRemove)
                {
                    flightToRemove.IsDeleted = true;
                    _flights.Remove(flightToRemove);
                    yield return flightToRemove;
                }

                var newFlights = GenerateAirports(1, Random.Shared.Next(3, 6));
                foreach (var flight in newFlights)
                    _flights.Add(flight);
            }

            var faker = new Faker();
            var flag = faker.Random.Int(1, 100);
            var flights = _flights
                .Where(flight => flight.Status is not (Statuses.LandedOnTime or Statuses.LandedDelayed or Statuses.Cancelled))
                .Where(flight => (flight.Departure - DateTime.UtcNow.TimeOfDay).TotalHours > 3)
                .ToArray();

            foreach (var flight in flights)
            {
                switch (flag)
                {
                    case 23:
                        //Statuses.ScheduledDelayed
                        flight.Status = Statuses.ScheduledDelayed;
                        flight.Departure = flight.Departure.Add(TimeSpan.FromMinutes(faker.Random.Int(30, 180)));
                        break;
                    case 42:
                        //Statuses.Cancelled
                        flight.Status = Statuses.Cancelled;
                        break;
                    default:
                    {
                        //Statuses.LandedOnTime | Statuses.LandedDelayed 
                        if (flight.Departure < DateTime.UtcNow.TimeOfDay)
                        {
                            flight.Status = flight.Status == Statuses.ScheduledDelayed ? Statuses.LandedDelayed : Statuses.LandedOnTime;
                        }

                        break;
                    }
                }

                yield return flight;
            }
        }
    }
}