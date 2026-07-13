#CLIP 460A - ELFE HÄLT BÖSEN-DIALOG

adaptive_sound changethemenow s460

sq_object summon Zwerg TriggerPos 6
call_method [Object 0] init
do_wait time 0.3
sq_actor find Zwerg 300 1 6
do_wait time 1

start_fade 0.1 0

sq_text file Urwald
sq_audio open Clip_460a

+sq_pen set BodenPos TriggerPos
+sq_pen move BodenPos {0 0 0}
+sq_pen set TalkPos TriggerPos
+sq_pen move TalkPos {0 3 0}

+sq_pen set KampfSchauPlatz TriggerPos
+sq_pen move KampfSchauPlatz {1 -1 0}

+sq_pen set EndePos BodenPos
+sq_pen move EndePos {0 -5 0}

+sq_pen set ElfPos TriggerPos
+sq_pen move ElfPos {0 -6 -2}
+sq_pen set ElfKopfPos TriggerPos
+sq_pen move ElfKopfPos {0 -13 -8};#0 -13 0
+sq_pen set ElfKopfZoomedPos TriggerPos
+sq_pen move ElfKopfZoomedPos {0 -13.3 0}
+sq_pen set ElfEndePos ElfPos
+sq_pen move ElfEndePos {0 3 0}
+sq_pen set TorPos BodenPos
+sq_pen move TorPos {-55 0 2};#x -4 z
+sq_pen set TorKameraPos TorPos
+sq_pen move TorKameraPos {5 0.3 0};#5 -0.5 0
+sq_pen set TorKamera2Pos TorKameraPos
+sq_pen move TorKamera2Pos {7 0 8};#6 0 8

+sq_pen set WiggleRollePos TorPos
+sq_pen move WiggleRollePos {0 -0.5 4}

+sq_pen set KameraStartSequenz TorPos
+sq_pen move KameraStartSequenz {-6 30 0}

+sq_pen set Wiggle1Pos TorPos
+sq_pen move Wiggle1Pos {-5 0 1}
+sq_pen set Wiggle2Pos TorPos
+sq_pen move Wiggle2Pos {-3.5 0 1.5}
+sq_pen set Wiggle3Pos TorPos
+sq_pen move Wiggle3Pos {-1.5 0 -2}
+sq_pen set Wiggle4Pos TorPos
+sq_pen move Wiggle4Pos {0 0 -3}
+sq_pen set Wiggle5Pos TorPos
+sq_pen move Wiggle5Pos {-2.5 0 0.5}
+sq_pen set Wiggle6Pos TorPos
+sq_pen move Wiggle6Pos {-0.5 0 1}

+sq_pen set WiggleBridge1Pos TorPos
+sq_pen move WiggleBridge1Pos {15 0 2}
+sq_pen set WiggleBridge2Pos TorPos
+sq_pen move WiggleBridge2Pos {15 0 2}
+sq_pen set WiggleBridge3Pos TorPos
+sq_pen move WiggleBridge3Pos {15 0 2}
+sq_pen set WiggleBridge4Pos TorPos
+sq_pen move WiggleBridge4Pos {15 0 2}
+sq_pen set WiggleBridge5Pos TorPos
+sq_pen move WiggleBridge5Pos {15 0 2}
+sq_pen set WiggleBridge6Pos TorPos
+sq_pen move WiggleBridge6Pos {15 0 2}

+sq_pen set WiggleKampf1BeamPos BodenPos
+sq_pen move WiggleKampf1BeamPos {-5.5 0 4}
+sq_pen set WiggleKampf2BeamPos BodenPos
+sq_pen move WiggleKampf2BeamPos {-7 0 6}
+sq_pen set WiggleKampf3BeamPos BodenPos
+sq_pen move WiggleKampf3BeamPos {-7 0 4.5}
+sq_pen set WiggleKampf4BeamPos BodenPos
+sq_pen move WiggleKampf4BeamPos {-6.9 0 6}
+sq_pen set WiggleKampf5BeamPos BodenPos
+sq_pen move WiggleKampf5BeamPos {-7 0 7}

+sq_pen set WiggleListen1Pos BodenPos
+sq_pen move WiggleListen1Pos {1 0 4.9}
+sq_pen set WiggleListen2Pos BodenPos
+sq_pen move WiggleListen2Pos {2.8 0 5.2}
+sq_pen set WiggleListen3Pos BodenPos
+sq_pen move WiggleListen3Pos {3.5 0 5.2}
+sq_pen set WiggleListen4Pos BodenPos
+sq_pen move WiggleListen4Pos {-1 0 4.7}
+sq_pen set WiggleListen5Pos BodenPos
+sq_pen move WiggleListen5Pos {-2 0 3.2}
+sq_pen set WiggleListen6Pos BodenPos
+sq_pen move WiggleListen6Pos {4.5 0 6.7}

###set_visibility [Getobjref Riesenelfe] 0
###set_visibility [Getobjref ElfenFluegelA] 0
###set_visibility [Getobjref ElfenFluegelB] 0
###set_visibility [Getobjref ElfenFluegelC] 0
###set_visibility [Getobjref ElfenFluegelD] 0

#do_wait time 0.05
###set_activegameplay [Getobjref Riesenelfe] 0
do_wait time 0.05

sq_object summon Riesenelfe_Sequenz ElfPos
do_wait time 0.2

sq_actor find Riesenelfe_Sequenz 300 1
###sq_object summon Dimensionstor TorPos
do_wait time 0.3
###set_roty [Object 1] -0.7

do_action beam WiggleRollePos 0

sq_object summon Zwerg Wiggle2Pos 6
call_method [Object 2] init
do_wait time 0.3
sq_object summon Zwerg Wiggle3Pos 6
call_method [Object 3] init
do_wait time 0.3
sq_object summon Zwerg Wiggle4Pos 6
call_method [Object 4] init
do_wait time 0.3
sq_object summon Zwerg Wiggle5Pos 6
call_method [Object 5] init
do_wait time 0.3
sq_object summon Zwerg TalkPos 7
call_method [Object 6] init
do_action rotate 5.6 0
do_wait time 0.3

set_visibility [Actor 0] 0
sq_actor find Zwerg 100 6 6
do_wait time 1.5
sq_actor find Zwerg 100 1 7

+start_fade 0.51 1
sq_camera fix KameraStartSequenz 2.5 0.7 0
do_wait time 0.5

sq_camera selset inout
sq_camera move TorKameraPos 1.7 -0.2 0.2 0.1
do_wait time 5
do_change muetze fight 0 auf noanim
do_change muetze fight 2 auf noanim
do_change muetze fight 3 auf noanim
do_change muetze fight 4 auf noanim
do_change muetze fight 5 auf noanim
do_wait time 3

do_action run WiggleBridge2Pos 2
do_action run WiggleBridge3Pos 3
do_action run WiggleBridge4Pos 4
do_action run WiggleBridge5Pos 5
do_wait time 2
set_visibility [Actor 0] 1
do_action anim mann.dimensionstor_end 0
do_wait time 0.5

do_action run WiggleBridge1Pos 0
do_wait time 3

#sq_camera fix TorKameraPos 1.3 -0.2 -0.2

#Fade in

#Wiggles kommen aus dem Dimensionstor

sq_camera move TorKamera2Pos 1.5 -0.2 0.25 0.2
do_wait time 3

#Wiggles kommen zum Elfenplatz, an dem der Monolog der Elfe abläuft.
sq_camera fix KampfSchauPlatz 1.2 -0.2 -0.3 0

do_action beam WiggleKampf1BeamPos 0
do_action beam WiggleKampf2BeamPos 2
do_action beam WiggleKampf3BeamPos 3
do_action beam WiggleKampf4BeamPos 4
do_action beam WiggleKampf5BeamPos 5
do_wait time 0.2

do_action run WiggleListen1Pos 0
do_action run WiggleListen2Pos 2
do_action run WiggleListen3Pos 3
do_action run WiggleListen4Pos 4
do_action run WiggleListen5Pos 5

#RiesenElfe sagt was: Kommt und spürt die neue Macht, die mich durchströmt!
do_wait time 1
sq_camera move KampfSchauPlatz 1.2 -0.2 0 0.3
do_wait time 2.5
do_action rotate ElfPos 4
do_action rotate ElfPos 5
do_action rotate ElfPos 0
sq_actor actionlist 1 {{anim talka} {anim talkb} {anim talkd} {anim talka} {anim talkb} {anim talkd} {anim talka} {anim talkb} {anim talkd} {anim talkc} {anim talkc} {anim talkc} {anim talkc} {anim talkc} {anim talkb} {anim talkd} {anim talkb} {anim talkc}}
do_action anim talka 1
do_text 460aa 6 {} Endlich Auto Off
do_wait time 1.5
do_action anim mann.lauschen_links 5
do_action rotate ElfPos 2
do_action rotate ElfPos 3
do_wait time 0.5
do_action anim lookup 0
do_action anim showup 4
do_action anim showup 2
do_action anim lookup 3
do_action anim lookup 5
do_wait time 0.5
sq_camera move ElfKopfPos 1.5 0.8 0 0.3
do_wait time 0.5

#Riesenelfe sagt was: Nun ist die Zeit gekommen. Nach all den Jahrtausenden | ist unser Zwist entschieden.

do_text 460ab 6 {} Auto Auto Off
do_wait time 2
#sq_camera move ElfKopfZoomedPos 0.7 -0.1 0 0.2
do_action rotate front 2
do_action rotate front 3
do_wait time 0.5
do_action beam WiggleListen2Pos 2
do_wait time 3.9

#Riesenelfe sagt was: Und wir Elfen triumphieren über Euch Maden! | Endlich nach all den Äonen hat Vater Odin eine weise Enstcheidung getroffen.
#do_action anim talka 1
do_text 460ac 6 {} Auto Auto Off
do_wait time 1.7
sq_actor eyes 2 { 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 3 c}
sq_actor eyes 3 { 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 c}
sq_actor mouth 2 { 3 }
sq_actor mouth 3 { 3 }
sq_actor actionlist 2 {{anim mann.stand_boese_loop} {anim mann.stand_boese_loop} {anim mann.stand_boese_end} {anim talkacngb} {anim insane} {anim warmbutt}}
sq_actor actionlist 3 {{anim insane} {anim talkacngb} {anim insane} {anim warmbutt} {anim insane} {anim talkacngb}}
do_action anim mann.stand_boese_start 2
do_action anim insane 3
do_wait time 0.2
+sq_pen set Schnitt1Pos 2
+sq_pen move Schnitt1Pos {0.5 -1 0}
sq_camera fix Schnitt1Pos 1.3 0.3 -0.6
do_wait time 6;#insgesamt 2.9+1.5

sq_camera fix ElfKopfPos 1.0 0.3 0
do_action rotate ElfPos 2
do_action rotate ElfPos 3
do_action rotate 4 5
do_action rotate front 4
do_wait time 0.5
#do_action anim talka 1
sq_actor actionlist 4 {{anim insane} {anim insane}}
sq_actor actionlist 5 {{anim talkacngb} {anim talkacngb}}
sq_actor mouth 4 {8}
sq_actor mouth 5 {4}
sq_actor eyes 4 {5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 c}
sq_actor eyes 5 {12 12 12 12 12 12 12 12 12 12 12 12 12 c}
do_action anim talkacngb 5
do_action anim insane 4
do_wait time 0.5
+sq_pen set Schnitt2Pos 5
+sq_pen move Schnitt2Pos {0.5 -4 0}
sq_camera fix Schnitt2Pos 1.8 -0.5 0.3
do_wait time 1.7

sq_camera fix ElfKopfPos 1.0 0.3 0
#Riesenelfe sagt was: #460ad Wiggles! Halbgötter, Ha! Ihr seid nichts. | Kommt doch und fordert mich endlich heraus.
do_text 460ad 6 {} Auto Auto Off
do_wait time 1

sq_camera move ElfKopfZoomedPos 0.8 0.4 0 0.3
do_action rotate ElfPos 4
do_action rotate ElfPos 5
do_action rotate front 0
do_wait time 1.5
sq_actor eyes 0 {2 2 2 2 2 2 2 2 2 2 2 2 2 2 9 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 9 2 2 2 2 9 2 9 2 9 2 9 2 c}
sq_actor mouth 0 {3}
do_wait time 0.1
do_action beam ElfEndePos 1
sq_camera fix 0 0.8 0.2 0
sq_actor actionlist 0 {{anim mann.angst_loop} {anim mann.angst_loop} {anim mann.angst_loop} {anim mann.angst_loop} {anim mann.angst_end}}
do_action anim mann.angst_start 0
sq_actor mouth 0 {3}
do_wait time 2
sq_camera move EndePos 2.2 0 0 0.4
do_wait time 2
do_action rotate ElfPos 0
do_wait time 1

#Der letzte Wiggle kommt aus dem Tor und die ersten schauen nach links
#Ein Wiggle schaut hoch, einer bekommt einen Schrecke, einer geht einen Schritt zurück
#Ein Wiggle setzt sich in tired Modus hin.

#Kamerafahrt die Elfe Aufwärts bis zum Gesicht und Elfe fängt schon an zu sprechen
#Kamerafahrt zum raus, während die Elfe weiterspricht
#Sobald die Wiggles wieder zu sehen sind machen die auch wieder was
#Nach dem letzten Satz der Elfe schießt sie mit einen Blitz nach unten der keinen trifft.
##############
#BLITZ
#lightning Quellposition Zielposition Richtungsvektor Farbwert1 Farbwert2 Farbwert3 Dauer
#create_particlesource 3 Ziel Richtungsvekor 1 1 ->kopieren -> ist ein Feuer beim Aufprall


+sq_pen set ElfHand1Pos ElfKopfPos
+sq_pen move ElfHand1Pos {-2.5 2 0}
+sq_pen set ElfHand2Pos ElfKopfPos
+sq_pen move ElfHand2Pos {2.5 2 0}
+sq_pen set TargetPos TriggerPos
+sq_pen move TargetPos {0 0 0}

do_action anim fire 1

+do_change muetze arbeitslos 0 ab noanim
do_wait time 0.4

+sq_pen set WiggleHit1Pos WiggleListen1Pos
+sq_pen move WiggleHit1Pos {0.1 0 0}
+sq_pen set WiggleHit2Pos WiggleListen2Pos
+sq_pen move WiggleHit2Pos {-0.1 0 0}
+sq_pen set WiggleHit3Pos WiggleListen3Pos
+sq_pen move WiggleHit3Pos {0.1 0 0}
+sq_pen set WiggleHit4Pos WiggleListen4Pos
+sq_pen move WiggleHit4Pos {-0.1 0 0}
+sq_pen set WiggleHit5Pos WiggleListen5Pos
+sq_pen move WiggleHit5Pos {0.1 0 0}

+lightning [parse_pos ElfHand1Pos] [parse_pos WiggleHit1Pos] [vector_sub WiggleHit1Pos ElfHand1Pos ] 0.3 0.1 0.2 0.8
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit2Pos] [vector_sub WiggleHit2Pos ElfHand2Pos ] 0.3 0.1 0.2 0.8
+lightning [parse_pos ElfHand1Pos] [parse_pos WiggleHit3Pos] [vector_sub WiggleHit3Pos ElfHand1Pos ] 0.3 0.1 0.2 0.8
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit4Pos] [vector_sub WiggleHit4Pos ElfHand2Pos ] 0.3 0.1 0.2 0.8
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit5Pos] [vector_sub WiggleHit5Pos ElfHand1Pos ] 0.3 0.1 0.2 0.8
do_wait time 0.1

+create_particlesource 3 [parse_pos WiggleHit1Pos] {0 -0.0 0} 1 1
+create_particlesource 3 [parse_pos WiggleHit2Pos] {0 -0.0 0} 1 1
+create_particlesource 3 [parse_pos WiggleHit3Pos] {0 -0.0 0} 1 1
+create_particlesource 3 [parse_pos WiggleHit4Pos] {0 -0.0 0} 1 1
+create_particlesource 3 [parse_pos WiggleHit5Pos] {0 -0.0 0} 1 1
set_particlesource [Actor 0] 0 1
set_particlesource [Actor 2] 0 1
set_particlesource [Actor 3] 0 1
set_particlesource [Actor 4] 0 1
set_particlesource [Actor 5] 0 1

+sq_pen move WiggleHit1Pos {0.1 0 0}
+sq_pen move WiggleHit2Pos {-0.1 0 0}
+sq_pen move WiggleHit3Pos {0.1 0 0}
+sq_pen move WiggleHit4Pos {-0.1 0 0}
+sq_pen move WiggleHit5Pos {0.1 0 0}

+lightning [parse_pos ElfHand1Pos] [parse_pos WiggleHit1Pos] [vector_sub WiggleHit1Pos ElfHand1Pos ] 0.3 0.1 0.2 0.5
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit2Pos] [vector_sub WiggleHit2Pos ElfHand2Pos ] 0.3 0.1 0.2 0.5
+lightning [parse_pos ElfHand1Pos] [parse_pos WiggleHit3Pos] [vector_sub WiggleHit3Pos ElfHand1Pos ] 0.3 0.1 0.2 0.5
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit4Pos] [vector_sub WiggleHit4Pos ElfHand2Pos ] 0.3 0.1 0.2 0.5
+lightning [parse_pos ElfHand2Pos] [parse_pos WiggleHit5Pos] [vector_sub WiggleHit5Pos ElfHand1Pos ] 0.3 0.1 0.2 0.5
do_wait time 0.5

start_fade 1 0

+do_change muetze fight 0 ab noanim
+do_change muetze fight 2 ab noanim
+do_change muetze fight 3 ab noanim
+do_change muetze fight 4 ab noanim
+do_change muetze fight 5 ab noanim

+do_action beam WiggleListen1Pos 0
+do_action beam WiggleListen2Pos 2
+do_action beam WiggleListen3Pos 3
+do_action beam WiggleListen4Pos 4
+do_action beam WiggleListen5Pos 5

#+del [Actor 0]
+sq_object delete all
###sq_camera get

+sq_camera fix TorKameraPos 1.7 -0.2 0.2 0.1
do_wait time 1
+sq_camera get
+sq_sound Nichts 0

#########################Test start
+do_action beam TriggerPos 0
#########################Test Ende

###+set_visibility [Getobjref Riesenelfe] 1
###+set_visibility [Getobjref ElfenFluegelA] 1
###+set_visibility [Getobjref ElfenFluegelB] 1
###+set_visibility [Getobjref ElfenFluegelC] 1
###+set_visibility [Getobjref ElfenFluegelD] 1
###+set_activegameplay [Getobjref Riesenelfe] 1

#460aa Kommt und spürt die neue Macht, die mich durchströmt!
#460ab Nun ist die Zeit gekommen. Nach all den Jahrtausenden | ist unser Zwist entschieden.
#460ac Und wir Elfen triumphieren über Euch Maden! | Endlich nach all den Äonen hat Vater Odin eine weise Enstcheidung getroffen.
#460ad Wiggles! Halbgötter, Ha! Ihr seid nichts. | Kommt doch und fordert mich endlich heraus.

+adaptive_sound changethemenow walhalla
+adaptive_sound marker walhalla [get_pos this] 1000

+cancel_fade

