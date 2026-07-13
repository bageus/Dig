#Clip 118 - Elfe warnt vor Kohle mitnehmen
+tasklist_clear [Actor 0]
sq_text file Schwefel
sq_audio open swf_118
#---inserted-by-Jan---MUSIC---------------
adaptive_sound changethemenow atmoschwefel
#-----------------------------------------

sq_camera selset inout

#sq_pen set naehe_Wachhaus TriggerPos
#sq_pen move nahe_Wachhaus {-23 0 0}
#sq_actor find Zwerg 5 1 1 naehe_Wachhaus
##sollte der Knocker vom Wachhaus sein
#sq_pen set Wachhaus 1

+sq_pen set Elfe1 TriggerPos
+sq_pen move Elfe1 {2 -1 2}
+sq_pen set Elfe0 Elfe1
+sq_pen move Elfe0 {3 0 0}
+sq_pen set zurueck TriggerPos
+sq_pen move zurueck {0.5 0 0}
+sq_pen set Kamera1 TriggerPos
+sq_pen move Kamera1 {1.5 -0.5 1.5}
+sq_pen set Knockerstart TriggerPos
+sq_pen move Knockerstart {-6.5 -1 0}
+sq_pen set Knockerback TriggerPos
+sq_pen move Knockerback {-5 -1 -2}
+sq_pen set Knockerziel TriggerPos
+sq_pen move Knockerziel {-0.6 0 0}
+sq_pen set Knockerende TriggerPos
+sq_pen move Knockerende {-10 -1 0}
+sq_pen set KameraaufzweiZwerge TriggerPos
+sq_pen move KameraaufzweiZwerge {-0.3 0 0}
+sq_pen set Kohlehaufen TriggerPos
+sq_pen move Kohlehaufen {0.5 0 0.5}
sq_color 0 Wiggle1

sq_wait all

sq_camera fix Kamera1 1.0 -0.5 0.5
sq_camera get
sq_object summon Zwerg Knockerstart 5
call_method [Object 0] Editor_Set_Info {{name Kohlebewacher} {gender male}}
call_method [Object 0] init
do_wait time 0.2
#do_change muetze fight 1 auf noanim
sq_actor find Zwerg 10 1 5
set_textureanimation [Actor 1] 0 7
set_textureanimation [Actor 1] 1 7
sq_actor express 1 bad_normal
sq_color 1 Knocker1
do_action walk TriggerPos 0
do_action rotate Kohlehaufen 0

sq_wait none
do_action anim offerjoint 0
do_elf path Elfe0 Elfe1
do_wait time 1
do_elf lookat 0
do_elf text 118a {} Sicherbedient
do_elf anim ablehnen
do_wait time 4
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
do_action walk Knockerziel 1
do_wait time 1.5
sq_actor eyes 1 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
do_elf text 118b {} Dieknockers
do_wait time 4.3
+do_elf hide

sq_camera fix KameraaufzweiZwerge 0.8 0.0 -0.2
do_wait time 0.8
do_action rotate 0 1
do_wait time 0.2
do_action anim mann.treten_hintern 1;                   # <--- ?!
do_wait time 0.2
do_action anim standfronthitm 0
do_text 118c 1 {NegAc} Ganzrecht
#"Ganz recht!"
do_action walk zurueck 0
do_wait time 1.5
do_action rotate 1 0
do_wait time 0.5
sq_pen set strafpredigt 1
sq_pen move strafpredigt {1 0 0}
sq_camera fix strafpredigt 0.7 -0.3 0.9
do_text 118d 1 {NegAc} Duglaubst
#"Du glaubst doch nicht..."
do_wait time 1.5
do_action anim standloopc 0
do_wait time 2
do_text 118e 1 {NegAc} UnserAnfuehrer
# "Unser Anführer hat..."
do_action anim scratchhead 0
do_wait time 1
do_action anim standloopb 0
do_wait time 2
do_action anim standloopa 0
do_wait time 2
do_action anim standloopb 0
do_wait time 2.5
do_action anim teeter_t 0
do_text 118f 1 {NegAc} Voneinem
#"Von einem..."
do_wait time 1
do_action anim standloopb 0
do_wait time 2
do_action anim standloopa 0
do_wait time 2
do_action anim standloopb 0

sq_pen set Elfe2 1
sq_pen move Elfe2 {-1 -0.5 2}
sq_pen set Elfe3 Elfe2
sq_pen move Elfe3 {0 0 2}
sq_camera fix 1 1.1 -0.4 -0.4
do_elf path Elfe3 Elfe2
do_wait time 1
do_elf lookat 1
do_wait time 1
do_elf text 118g {} Ichdachte
#"Ich dachte..."
do_wait time 1
do_action rotate Elfe2 1
do_wait time 1.5

do_text 118h 1 {NegAc} Papperlapappder {50 10}
#"Papperlapapp!..."
do_wait time 6

+do_elf hide
sq_camera fix KameraaufzweiZwerge 0.9 -0.45 -1.0
sq_actor eyes 1 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
do_action rotate 0 1
do_wait time 1
sq_actor eyes 0 {c c c c c c 9 c c c c c c c c c c c 9 c c c c 9 c c c c c c c c c c c c c c c c c 9 c c c c c c c c c c c c c 9 c c c c c c c c c c c 9 c c c 9 c c c c c c c c c c 9 c c c c c c c c c c c c 9 c c c c c c c c c c 9 c c c c c c c 9 c c c c c c c c c c c 9 c c}
do_action anim scratch 0
do_wait time 2

do_action walk Knockerback 1
do_wait time 1.5
sq_camera fix 0 0.9 0.05 0.3
do_wait time 0.5
do_text 118i 0 {NegAc} Schlimmschlimm
do_wait time 2
sq_camera fix KameraaufzweiZwerge 1 -0.45 -1.0
do_action rotate 0 1
do_wait time 1
do_action anim standloopa 1
do_action anim standloopc 0
do_wait time 1
do_action anim standloopc 1
do_wait time 1
do_action anim standloopd 0
do_text 118j 1 {Auto} Siehdich
do_wait time 2
do_action anim standloopc 0
do_wait time 3
do_action walk Knockerstart 1
do_wait time 2

sq_camera fix Kamera1 1.0 -0.5 0.5
+sq_object delete all
#---inserted-by-Jan---MUSIC---------------
+adaptive_sound changethemenow knockers
#-----------------------------------------

