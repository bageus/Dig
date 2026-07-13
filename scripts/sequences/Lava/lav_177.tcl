sq_text file Lava
sq_audio open lav_177
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow s177
#-----------------------------------------
sq_wait all
set_autolight [Actor 0] 0
sq_pen set vorTrigger TriggerPos
sq_pen move vorTrigger {1.8 -1 0}
sq_pen set etwasrechtsvomTrigger TriggerPos
sq_pen move etwasrechtsvomTrigger {0.2 0 0}
sq_pen set woZweitererscheint TriggerPos
sq_pen move woZweitererscheint {-2 0 -1.5}
sq_pen set woZweiterankommt TriggerPos
sq_pen move woZweiterankommt {0.5 0 -1.5}
sq_pen set woZweitererschrickt TriggerPos
sq_pen move woZweitererschrickt {5 0 -1.5}
sq_pen set Elfe1 TriggerPos
sq_pen move Elfe1 {1.8 0 1.8}
sq_pen set Elfe1Kam Elfe1
sq_pen move Elfe1Kam {-0.4 -0.5 -0.4}
sq_pen set Elfe2 TriggerPos
sq_pen move Elfe2 {15 -10 0}
sq_pen set Elfe3 TriggerPos
sq_pen move Elfe3 {16 -1 0}
sq_wait all
sq_camera selset inout

sq_pen set marker1 TriggerPos
sq_pen move marker1 {14.87 0 0}
sq_pen set marker2 TriggerPos
sq_pen move marker2 {55.12 13 0}
sq_pen set marker3 TriggerPos
sq_pen move marker3 {43.5 32 0}
sq_color 0 Wiggle1


sq_pen move vorTrigger {0 0.5 0}
sq_camera move vorTrigger 1.0 0.1 0.9 0.2
sq_pen move vorTrigger {0 -0.5 0}
sq_actor express 0 bad_normal
do_action walk TriggerPos 0
do_action rotate right 0
do_wait time 2
sound play vampir_unheimlich 1
do_wait time 4

sq_pen move vorTrigger {-0.5 0 0}
sq_camera fix vorTrigger 0.9 -0.8 -0.2 0.2
sq_pen move vorTrigger {0.5 0 0}
sq_object summon Zwerg woZweitererscheint 5
set_autolight [Object 0] 0
call_method [Object 0] Editor_Set_Info {{gender male}}
call_method [Object 0] init
sq_actor find Zwerg 5.0 1 5 woZweitererscheint
sq_actor express 1 bad_normal
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
sq_actor eyes 1 {c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c c c c 9 c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c 9 c c c c c c c c c c c c c c c c c c c c c c c c 9 c c c c c c }
do_change muetze arbeitslos 1 auf noanim
sq_color 1 Wiggle2

sq_wait none
do_action walk woZweiterankommt 1
do_action anim mann.lauschen_links 0
do_wait time 1
do_text 177a 1 {NoAnim} Uppswo
do_action walk woZweiterankommt 1
do_wait time 2
do_action anim talkacnta 1
do_wait time 1.3
do_action anim talkacntc 1
do_wait time 0.7
do_action anim talkacntb 1
do_wait time 1
sq_wait all
do_action rotate right 1
sq_wait none
#...Auto {30,30}
do_action anim leftright 0
do_action anim talkacntc 1
do_wait time 1
do_action anim talkacntb 1
do_wait time 1
do_action anim kontrol 0
sq_wait all
do_wait time 1
sq_wait none
do_text 177b 0 {{talkacnta} {talkacntb}} Hoermal
do_action rotate 0 1
do_wait time 2
do_action rotate right 1
sq_wait all
do_wait time 1
sq_wait none
sq_camera move etwasrechtsvomTrigger 0.7 0.05 -0.4 0.15
do_text 177c 1 {{talkacntb} {scratchhead} {talkacnta}} Jairgendwas
do_wait time 1
do_action walk woZweitererschrickt 1
do_wait time 2
do_action rotate back 0
do_wait time 0.5
do_action anim scout 0
do_wait time 2
do_action rotate right 0
do_wait time 0.5
do_action rotate 0 1
#umdrehen, muss ja gleich wieder zurück...
do_wait time 0.5
do_text 177e 1 {NoAnim} Schrei
do_action anim washface 0
do_wait time 2

sq_wait all
sq_wait none
sq_camera fix 0 0.9 0.05 -0.4
do_text 177d 0 {{shock} {showup} {impatient}} Waswo
#actor 1 flieht auf actor 0 zu
do_action panicflee 0 1
do_wait time 1.5
#sq_camera fix 0 0.7 0.05 0.4

do_wait time 0.3
do_action anim standfronthith 0
sq_pen move woZweitererscheint {-10 0 0}
do_action panicflee woZweitererscheint 1
do_wait time 1
#sq_camera move 0 1.1 -0.2 0.5 0.3
sq_pen move Elfe1Kam {0 0.5 0}
sq_camera fix Elfe1Kam 1.0 -0.1 -0.8;#1.0 0.1 0.1
sq_pen move Elfe1Kam {0 -0.5 0}
do_wait time 1
do_action rotate woZweitererscheint 0
do_wait time 1
#er guckt ihm noch nach

do_elf move Elfe3
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c }
do_text 177e 0 {Auto} Beimodin
do_elf lookat 0
do_wait time 1.5
+do_change muetze transport 1 ab noanim
do_wait time 0.5
+sq_object delete all
sq_pen set ElfeSeite Elfe1
sq_pen move ElfeSeite {0.5 -0.2 -0.5}
do_elf path Elfe3 ElfeSeite
do_wait time 1
do_action rotate ElfeSeite 0
do_wait time 1
do_text 177f 0 {NegAc} Duwas
do_wait time 3.5
sq_wait 0
do_elf text 177g {} Ichich
do_wait time 0.5
do_elf anim kopf_schuetteln
do_wait time 3
sq_camera fix etwasrechtsvomTrigger 0.75 -0.25 -0.5
do_text 177h 0 {NegAc} Neinnein
do_wait time 1
sq_camera fix Elfe1Kam 1.1 -0.1 0.0
do_elf text 177i {} Quatschmir
do_wait time 1
do_elf anim ablehnen
do_wait time 2
do_elf anim kopf_schuetteln
do_wait time 5
do_elf text 177j {} Hiermuessen
do_wait time 2
do_elf anim zeigen_links
do_wait time 2
sq_camera fix etwasrechtsvomTrigger 0.75 -0.25 -0.5
do_text 177k 0 {NegReac} Boesezwerge
do_wait time 1.5
sq_pen set ElfeQuatsch ElfeSeite
sq_pen move ElfeQuatsch {-1 0 -1}
sq_camera fix ElfeQuatsch 1 -0.2 0.5;#move 0 1.2 -0.2 0.5 0.6
do_elf text 177l {} Seinicht
do_elf anim ablehnen
do_wait time 2

sq_pen set letzteKam 0
sq_pen move letzteKam {4 -2 -4}
sq_camera fix letzteKam 1.5 -0.25 -0.5
do_elf lookat
do_elf move Elfe2
do_wait time 1
do_text 177n 0 {{shock} {talkacntb} {talkacnta}} Heybleib
+elfenarbeit1
+elfenarbeit2
+do_elf hide
#do_elf move Elfe2
#do_wait time 1
#do_action rotate Elfe2 0
#sq_camera fix vorTrigger 1.0 0.25 1.1
#do_wait time 3
#do_particle create 13 Elfe2 {0.0 -0.1 0.0} 40 4
#+elfenarbeit1
#erster von zwei FogRemover Funktionsaufrufen
#do_wait time 0.5
#do_elf move Elfe3
#do_wait time 1
#do_text 177m 0 {NoAnim} Deinwort
#do_wait time 2.5
#do_particle create 13 Elfe3 {0.0 -0.1 0.0} 40 4
#+elfenarbeit2
#+do_elf hide
#do_wait time 2
#sq_camera fix vorTrigger 1.0 -0.2 0.0
#do_text 177n 0 {{shock} {talkacntb} {talkacnta}} Heybleib
sq_wait all
+set_autolight [Actor 0] 1
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound marker mauls [parse_pos marker1]
+adaptive_sound marker mauls [parse_pos marker2]
+adaptive_sound marker mauls [parse_pos marker3]
+adaptive_sound changethemenow mauls
#-----------------------------------------

