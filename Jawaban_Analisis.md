# Praktikum 7 — Squad Tactical Enemy AI

**Nama Kelompok:** **Choom** 
**Anggota:** 
- Triana Velia Hutabalian (5025231190)
- Lailatul Annisa Fitriana (5025231202)
- Rafaela Shyra Ashma' Ramadhani (5025231217)

Project: `GC07_TacticalSquadAI` · Scene: `Assets/Scenes/Praktikum07.unity` · Unity 6000.0.83f1

## Cara Main

| Tombol | Fungsi |
|---|---|
| **W A S D** / panah | Gerakkan Player (kapsul biru) |
| **V** | Visual debug ON/OFF (kerucut FOV, garis tujuan, penanda tujuan, garis ke cover) |
| **T** | Shared Information ON/OFF (Eksperimen 3) |
| **K** | Isi ulang health Player |
| **R** | Restart scene |

Scene dibangun ulang lewat menu **Praktikum 07 → Build Scene**.

## Susunan Sistem

```
SquadManager ── Shared Target · Shared Last Known Position · Alert · daftar anggota
CoverManager ── FindBestCover · IsProtected · HasFiringLine
│
├── Enemy_01_Attacker      ┐ EnemyPerception  (FOV + Raycast)      = apa yang DILIHAT
├── Enemy_02_Flanker       │ EnemyMemory      (last known + timeout) = apa yang DIINGAT
└── Enemy_03_CoverShooter  │ EnemyTacticalAI  (role + keputusan)    = apa yang DILAKUKAN
                           │ EnemyCombat      (tembak + cooldown)
                           │ NavMeshAgent     (gerak)               = BAGAIMANA menuju posisi
                           ┘ EnemyDebugVisual · TacticalNameplate
```

Pipeline per Enemy: **Sense → Remember → Share → Evaluate (tiap 0,35 dtk) → Pilih aksi sesuai Role → Pilih posisi taktis → Navigate → Attack / Search → Re-evaluate.**

## Perilaku yang Dihasilkan

| Role | Saat Player terlihat oleh squad | Posisi taktis |
|---|---|---|
| Attacker | Mendekat dari depan sampai jarak ideal 6 m **dan** punya garis pandang | jarak tempur ideal |
| Flanker Left | Bergerak ke sisi kiri Player (`target.right × 5 × −1`) | titik flank, divalidasi `NavMesh.SamplePosition` |
| Cover Shooter | Memilih Cover Point, mereservasi, bergerak ke sana, menembak dari balik crate | `CoverManager.FindBestCover` |
| Semua | Player hilang → **Search** ke posisi terakhir; 8 dtk tanpa laporan → **Calm** → kembali ke pos jaga | last known position (shared) |

## Perbedaan dari Modul (perbaikan yang disengaja)

| # | Modul | Implementasi ini | Alasan |
|---|---|---|---|
| 1 | `EnemyMemory`: `LastSeenTime` default 0 | default `-∞` | Dengan nilai 0, `Time.time − 0 ≤ 5` membuat Enemy "mengingat" Player palsu pada 5 detik pertama. |
| 2 | Enemy memakai `target.position` (posisi asli Player) sampai `SharedTarget` hangus 8 dtk | Role dijalankan hanya saat ada **live contact**; jika tidak, seluruh squad **Search** ke last known position | Pada kode modul state `Search` praktis tidak pernah tercapai dan Enemy "tahu" ke mana Player lari. |
| 3 | Laporan memakai `player.position` saat ini | Laporan memakai posisi **saat terakhir terlihat** (`SeenPlayerPosition`) | Cache perception terlambat satu frame; tanpa ini Player yang berpindah mendadak ikut "terbawa" ke laporan. |
| 4 | Attacker berhenti di jarak ideal | Berhenti hanya jika jarak ideal **dan** Player terlihat | Tes: Attacker berhenti di balik Wall_03 pada jarak 5 m tanpa bisa melihat Player. |
| 5 | Cover dipilih sekali dan dipertahankan | Cover divalidasi ulang tiap evaluasi; jika Player berpindah dan cover tak lagi melindungi → reservasi dilepas, pilih ulang | Cover bisa menjadi tidak berguna saat Player bergerak. |
| 6 | Cover hanya diskor dari jarak | Skor = 0,5 × punya garis tembak + 0,5 × kedekatan; wajib terlindung dan berjarak 3–14 m dari Player | Cover di balik tembok tinggi membuat Cover Shooter tidak bisa menembak sama sekali. |
| 7 | Hanya layer Obstacle | Ditambah layer **Cover** untuk crate rendah (1,1 m) | Crate melindungi saat jongkok (Raycast setinggi 0,5 m terhalang) tetapi tidak menghalangi pandangan (mata di 1,5 m), sehingga Cover Shooter benar-benar menembak dari balik crate. Tembok tinggi (Obstacle) menghalangi keduanya. |
| 8 | `Input.GetAxisRaw`; gerak lewat `transform.position` | `Keyboard.current` + `CharacterController` | Project memakai Input System baru; Player tidak menembus Wall/Crate. |
| 9 | Fire hanya `Debug.Log` | Log + tracer (hitscan) + damage kecil dengan `hitChance` 50% dan `reactionDelay` 0,4 dtk | Efek serangan terlihat; Enemy tidak sempurna (sesuai catatan §78 modul). |
| 10 | Tidak ada | Enemy kembali ke pos jaga saat Calm; saat Search tiap anggota menuju titik yang sedikit berbeda | Mencegah Enemy menetap di tempat acak dan menumpuk di satu titik. |

Fitur tambahan: HUD Squad (status Calm/Alert, shared data, state tiap anggota, reservasi cover), nameplate Role + State di atas kepala, kerucut FOV di lantai, health Player + efek layar merah + angka damage.

## Jawaban Tugas Pengamatan

**1. Perbedaan Perception, Memory, Decision, Navigation?**
- **Perception** (`EnemyPerception`): apa yang *terlihat sekarang* — jarak, FOV, Line of Sight. Tidak menyimpan apa pun.
- **Memory** (`EnemyMemory`): apa yang *diketahui* — posisi terakhir Player dan batas waktu ingatan (5 dtk), meski Player sudah tidak terlihat.
- **Decision** (`EnemyTacticalAI`): apa yang *harus dilakukan* — memilih aksi sesuai Role dan kondisi squad.
- **Navigation** (`NavMeshAgent`): *bagaimana menuju* posisi yang sudah dipilih; mencari jalur dan bergerak.

**2. Mengapa SquadManager dibutuhkan?**
Sebagai shared blackboard. Tanpa itu, hanya Enemy yang melihat Player yang tahu; yang lain tetap menjaga. Dengan `ReportPlayerSeen()`, satu laporan membuat seluruh squad Alert dan mengetahui posisi Player sehingga bisa berkoordinasi (satu menekan, satu mengapit, satu berlindung).

**3. Apa yang terjadi jika shared information dimatikan?**
Tekan **T** (atau matikan `shareWithSquad`). Tiap Enemy hanya bereaksi pada perception dan memory-nya sendiri: Enemy yang tidak melihat Player tetap Guard, Enemy yang melihat bergerak sendirian, tidak ada Alert squad, dan tidak ada koordinasi role. Squad bertindak seperti tiga AI individual.

**4. Mengapa Cover Point perlu status Occupied?**
Untuk Cover Reservation. Tanpa reservasi, semua Cover Shooter memilih cover terbaik yang sama dan menumpuk di satu titik. Dengan `TryReserve()` / `OccupiedBy`, cover yang sudah diambil dilewati sehingga Enemy tersebar ke cover berbeda. Reservasi dilepas lewat `Release()` ketika Enemy berganti rencana (Calm, Search, cover tak lagi melindungi) atau dinonaktifkan.

**5. Mengapa titik flank harus diperiksa dengan `NavMesh.SamplePosition()`?**
Titik hasil rumus `Player + right × 5` hanyalah titik matematis; bisa berada di dalam tembok, di atas crate, atau di luar map, sehingga `NavMeshAgent` tidak bisa mencapainya. `SamplePosition` mencari titik NavMesh valid terdekat; jika tidak ada, Flanker memakai fallback (menyerang langsung) agar AI tidak macet.

**6. Perbedaan NavMesh pathfinding dan Tactical positioning?**
Pathfinding menjawab *"bagaimana mencapai posisi X?"* (jalur terpendek menghindari tembok). Tactical positioning menjawab *"posisi mana yang sebaiknya dicapai?"* (CoverPoint_04, sisi kiri Player). Contoh: Tactical AI memilih `CoverPoint_02`, NavMesh mencari jalur ke sana.

**7. Mengapa tactical decision tidak sebaiknya dilakukan setiap frame?**
Pada 60 FPS itu 60 keputusan/detik per Enemy, boros CPU (terutama dengan Raycast dan pencarian cover) dan membuat perilaku berkedip-kedip karena berganti rencana terus. Dengan `decisionInterval = 0,35` hanya ±3 keputusan/detik, perilaku lebih stabil, dan terasa seperti reaction time yang wajar. Pergerakan tetap mulus karena `NavMeshAgent` berjalan tiap frame. Tiap Enemy diberi offset acak agar tidak menghitung pada frame yang sama.

**8. Semua Enemy Attacker vs role dibagi?**
Jika semua Attacker, mereka datang dari arah yang sama dan menumpuk di depan Player — mudah diprediksi dan dihindari. Dengan role terbagi, squad menekan dari depan, mengapit dari sisi, dan menembak dari balik cover secara bersamaan: Player tidak punya satu arah aman, dan perilaku tampak terkoordinasi walau tiap aturannya sederhana.

## Pertanyaan Analisis — Sistem A (semua Chase) vs Sistem B (Attack / Flank / Cover)

- **Lebih cerdas:** B. Tiap Enemy punya peran dan posisi taktis berbeda; A hanya tiga kejar-kejaran yang sama.
- **Lebih menantang:** B, karena Player diserang dari beberapa sisi dan tidak ada satu rute kabur yang aman. A mudah diatasi dengan menggiring semua Enemy ke satu arah.
- **Lebih mudah diprediksi:** A. Semua berperilaku sama dan bergerak langsung ke Player. Pada B, perilaku bergantung Role, posisi Player, dan ketersediaan cover.
- **Apakah Tactical AI selalu lebih sulit?** Tidak. Taktik yang buruk (flank yang lambat, cover yang tidak berguna, tembakan yang `hitChance`-nya rendah) bisa lebih mudah dikalahkan. Kesulitan ditentukan oleh parameter tuning, bukan sekadar adanya taktik.
- **Apakah AI yang terlalu optimal selalu lebih menyenangkan?** Tidak. AI yang sempurna terasa tidak adil. Karena itu dipakai batasan: FOV terbatas, memory timeout, interval keputusan, jeda reaksi tembak, dan peluang tembakan mengenai 50%. Tujuannya perilaku yang meyakinkan, dapat dipahami, menantang, dan menyenangkan.

## Panduan Eksperimen

| Eksperimen | Cara |
|---|---|
| 1. View Radius 6 / 12 / 20 | Ubah `View Radius` pada `EnemyPerception` ketiga Enemy; amati seberapa cepat Alert. |
| 2. View Angle 45 / 90 / 180 / 360 | Ubah `View Angle`. Pada 360° Enemy melihat ke belakang sehingga hampir mustahil menyelinap. |
| 3. Shared Memory | Tekan **T** saat Play. Hanya Enemy yang melihat Player yang bereaksi. |
| 4. Jumlah Cover | Hapus sebagian elemen `Cover Points` di `CoverManager`; dengan 1 cover, hanya satu Cover Shooter yang berlindung, sisanya fallback. |
| 5. Decision Interval 0,1 / 0,35 / 1 / 2 | Ubah `Decision Interval` pada `EnemyTacticalAI`; makin besar makin lambat bereaksi. |
| 6. Semua Attacker | Ubah `Role` ketiga Enemy menjadi `Attacker`; mereka menumpuk dari arah yang sama. |


## Checklist Implementasi

- [x] Project `GC07_TacticalSquadAI`, scene `Praktikum07`
- [x] Ground, Player, 3 Enemy, Wall_01–03, Crate_01–04, 6 Cover Point
- [x] Layer Player, Enemy, Obstacle, Ground (+ Cover)
- [x] AI Navigation terpasang, NavMeshSurface di-bake
- [x] NavMeshAgent, EnemyPerception, EnemyMemory, EnemyTacticalAI, EnemyCombat, EnemyDebugVisual pada tiap Enemy
- [x] SquadManager (shared target, last known, alert) dan CoverManager (reservasi)
- [x] Role Attacker / Flanker / Cover Shooter
- [x] Debugging lewat Scene View (Gizmos), Game View (visual debug + HUD), dan Inspector
