sq_text file Urwald

sq_audio open Urw_007

#---inserted-by-Jan---MUSIC---------------
adaptive_sound markerdisable
adaptive_sound changethemenow firstcontact
#-----------------------------------------
+sq_activate Zwerg range 50 limit 2
+sq_wait none
+sq_pen set P_Start TriggerPos
+sq_pen move P_Start { 0 5 -10 }
sq_actor find Zwerg 300 2 1
do_action walk TriggerPos 0
do_change muetze sparetime 1 auf noanim
do_change muetze sparetime 2 auf noanim
sq_wait none
+sq_pen set kamera8 TriggerPos
+sq_pen move kamera8 {1 0 0}
sq_camera fix kamera8 0.95 -0.1 0.5 3
do_wait time 0.6
sq_color 2 {255 205 73}
+sq_pen set wiggle1 [Getobjpos Info_Pos_Troll 0]
+sq_pen set wiggle2 TriggerPos
+sq_pen set kiffen1 wiggle1
+sq_pen set kiffen2 wiggle1
+sq_pen set kamera wiggle1
+sq_pen set kamera2 wiggle1
+sq_pen set elfe wiggle1
+sq_pen set elfestart wiggle1
+sq_pen move wiggle2 {-3 0 0}

+sq_pen move kiffen1 {0.8 0 2.5}

+sq_pen move kiffen2 {2 0 1.5}
+sq_pen move kamera {1 -0.3 2}
+sq_pen move kamera2 {3 -0.3 2}
+sq_pen set kamera3 kamera2
+sq_pen move kamera3 {1 0 0}
+sq_pen move wiggle1 {3.5 0 2}
+sq_pen move elfe {4.5 -0.5 6}
+sq_pen set elfende elfe
+sq_pen move elfende {-10 0 3}
+sq_pen set elfencam elfe
+sq_pen move elfencam {0 -0.5 0}
+sq_pen move elfestart {0 5 3}
do_action beam kiffen1 1
+sq_pen move kiffen1 {-0.8 0 0}
do_action rotate right 1
#sq_pen move kiffen1 {-0.8 0 0}
do_action beam kiffen2 2
do_action rotate left 2
sq_wait 0
sq_object summon Dummy_Joint_a
do_wait
link_obj [Object 0] [Actor 2] 0
do_action rotate left 0
do_action anim wipenose 0
sq_wait none
sq_actor actionlist 0 {{walk wiggle1} {anim cough}}
do_action rotate left 0
do_wait time 2
sq_camera fix kiffen2 1.1 -0.1 -0.7


sq_wait none
do_action anim smokepot 2
do_wait time 2.6
change_particlesource [Actor 2] 0 6 {0 -0.02 0} { 0 0.02 0.02 } 100 3 0 10 0.4
set_particlesource [Actor 2] 0 1
do_wait time 0.5
set_particlesource [Actor 2] 0 0
sq_wait 2
do_wait

do_action rotate 1 2
sq_wait 1
do_action anim offerjointa 2
do_action anim takejointa 1
link_obj [Object 0] [Actor 1] 0
do_action anim offerjointb 2
do_action anim takejointb 1
sq_wait none
sq_actor actionlist 2 {{anim leanstart} loopstart {anim leanloop} loop}
+sq_pen set Endpunkt kiffen1
do_action rotate left 2
sq_actor actionlist 1 {{anim djhigh} {anim smokepot} {anim djhigh} {anim sitdown} {anim standup} {walk kiffen1} {rotate left} {anim sitdown} {anim sittosleep} loopstart {anim sleepside} loop}
sq_wait none
do_action anim smokepot 1
do_wait time 2.6
change_particlesource [Actor 1] 0 6 {0 -0.02 0} { 0 0.02 0.02 } 100 3 0 10 0.4
set_particlesource [Actor 1] 0 1
do_wait time 0.5
set_particlesource [Actor 1] 0 0
sq_wait 0
do_wait

sq_actor actionlist 2 {}
sq_wait none
do_action anim shock 2

do_text 007aa 0 Auto 007aa

sq_wait 2
do_wait time 0.7
sq_actor express 1 good_sleep
link_obj [Object 0]
global P_Start; set_pos [Object 0] $P_Start
sq_camera fix 2 1 -0.1 -0.5
do_action rotate 0 2
do_action anim tooltakeout_a 2
sq_object summon Dummy_Joint_a
do_wait
link_obj [Object 1] [Actor 2] 0
do_action anim tooltakeout_b 2

sq_wait 2
sq_camera fix 2 0.8 -0.1 -0.9

do_text 007ab 2 { talkacntc } 007ab

do_text 007abb 2 { offerjointa offerjointb } 007abb

sq_camera fix 2 1.2 -0.125 0.7
sq_wait 0

do_text 007ac 0 { talkrengb } 007ac

sq_camera fix 2 0.9 -0.075 -0.8
sq_wait 2

do_text 007a 2 Auto 007a
sq_camera move 2 0.7 -0.1 0
do_action rotate front 2
sq_wait 2
sq_actor eyes 2 { c c c c c c 13 13 13 13 c c c c c 13 13 13 13 c }
do_text 007c 2 Auto 007c {30 20 }
sq_camera selset inout
sq_camera move kamera2 1.2 -0.1 0.05 0.3
do_action rotate 0 2
sq_wait none
do_text 007d 2 Auto 007d
do_elf beam elfestart
do_wait time 2.5
sq_actor actionlist 0 {{{anim standloopa} {anim standloopa} {anim standloopa} {anim standloopa} {anim standloopb} {anim standloopc} {anim scratchhead} {anim scratch} {anim wipenose}} loop}
do_action anim standloopb 0
sq_wait 2
do_elf move elfe
sq_wait elf
do_action rotate elfe {0 2}
sq_wait none
#sq_sound 007f 0
do_elf text 007f Auto 007f
sq_wait none
do_wait time 2
sq_camera fix kamera3 1 -0.15 0.7
do_wait time 1
do_elf move elfende
do_wait time 2
sq_camera fix 2 1 -0.13 -0.4
+do_elf hide
sq_wait 2
do_text 007h 2 { scout scratchhead } 007h
do_action rotate 0 2
sq_actor actionlist 0 {}
sq_wait 0
do_text 007ad 0 { talkacngb } 007ad
sq_wait none
sq_actor actionlist 0 { {rotate 2} loopstart {{anim standloopa} {anim standloopa} {anim standloopa} {anim standloopa} {anim standloopb} {anim standloopc} {anim scratchhead} {anim scratch} {anim wipenose}} loop}
do_action anim standloopc 0
sq_wait none
do_action anim smokepot 2
do_wait time 2.6
change_particlesource [Actor 2] 0 6 {0 -0.02 0} { 0 0.02 0.02 } 100 4 0 10 0.4
set_particlesource [Actor 2] 0 1
do_wait time 0.5
set_particlesource [Actor 2] 0 0
sq_wait 2
do_wait
do_text 007ae 2 Auto 007aeb
#sq_wait none
#do_action rotate 2 0
#sq_wait 2
#do_text 007i 2 Auto Hmm_i
#do_wait time 1.5
#do_text 007j 2 Auto HatDich_j
+sq_pen move kamera {1 0 0}
#do_wait time 1
#sq_wait none
+sq_pen move kiffen1 {1 0 0.5}
+sq_pen move kiffen2 {1 0 1}
+sq_pen set kamera7 kamera
+sq_pen move kamera7 {0 0.5 0}
sq_camera fix kamera7 0.9 -0.1 0.7
#sq_actor actionlist 0 {{anim kungfustillani} {anim kungfustillani} {anim kungfutostand}}
#do_action anim standtokungfu 0
#sq_wait 2
#do_text 007k 2 Auto SchoenDumm_k
#sq_wait 0
#do_text 007l 0 Auto HabtIhr_l
sq_wait 2
#sq_actor actionlist 0 {{{anim standloopa} {anim standloopa} {anim standloopa} {anim standloopa} {anim standloopb} {anim standloopc} {anim scratchhead} {anim scratch} {anim wipenose}} loop}
#do_text 007m 2 Auto CoolAlter_m
do_action walk kiffen1 2
#sq_wait none
+sq_pen set kamera6 kiffen1
+sq_pen move kamera6 {0.5 0 0}
#sq_camera fix kamera 1 -0.1 0.3
#sq_wait 2
+sq_pen set kamera4 0
+sq_pen move kamera4 {-2.5 0 0}
sq_camera move kamera4 0.9 -0.1 0.7 0.3
do_action rotate 0 2
sq_actor actionlist 0 {{walk kiffen2} {rotate 2}}
do_action anim scratchhead 0
sq_wait 2
do_text 007n 2 Auto 007n
do_text 007o 2 Auto 007o
sq_camera fix kamera4 1 -0.1 -0.8
do_text 007p 2 Auto 007p

sq_wait none
do_action anim smokepot 2
do_wait time 2.6
change_particlesource [Actor 2] 0 6 {0 -0.02 0} { 0 0.02 0.02 } 100 4 0 10 0.4
set_particlesource [Actor 2] 0 1
do_wait time 0.5
set_particlesource [Actor 2] 0 0
sq_wait 2
do_wait

do_text 007q 2 Auto 007q

sq_camera fix kamera4 0.9 -0.1 0.9
sq_wait none
sq_actor actionlist 2 {{anim djhigh} {anim protecteyesstart} {anim protecteyesstop} loop}
do_action anim smokepot 2
do_wait time 2.6
change_particlesource [Actor 2] 0 6 {0 -0.02 0} { 0 0.02 0.02 } 100 4 0 10 0.4
set_particlesource [Actor 2] 0 1
do_wait time 0.5
set_particlesource [Actor 2] 0 0
do_wait
sq_actor actionlist 0 {}
sq_wait 0
do_action rotate 2 0
sq_actor actionlist 2 {}
sq_wait 2
+sq_pen move kiffen2 {-1.5 0 0}
+sq_pen move kiffen1 {-0.5 0 0}
do_action rotate left 2
do_action walk kiffen1 2
set_roty [Actor 2] 1.57
+sq_pen move kiffen1 {-1 0 0}
+sq_pen set kamera4 0
+sq_pen set kamera5 0
+sq_pen move kamera4 {-2 0 0}
+sq_pen move kamera5 {-0.25 0 0}
sq_wait 2
sq_camera move kamera4 0.9 -0.05 0.8
#do_wait time 0.5
link_obj [Object 1]
global P_Start;set_pos [Object 1] $P_Start
do_action anim stummble 2
sq_camera fix kamera5 0.8 -0.1 0.7
sq_wait 0
do_text 007s 0 Auto 007s
+sq_pen move kiffen1 { -0.5 0 0 }
do_action beam kiffen1 2
sq_wait none
set_roty [Actor 2] 0.4
sq_actor actionlist 0 {{{anim standloopa} {anim standloopa} {anim standloopa} {anim standloopa} {anim standloopb} {anim standloopc} {anim scratchhead} {anim scratch} {anim wipenose}} loop}
do_action anim standloopb 0
sq_wait 2
sq_camera fix kamera4 0.9 -0.05 0.8
do_action anim standup 2
do_action rotate 0 2
#sq_actor actionlist 1 {{anim sleepside} loopstart}
#do_action anim sittosleep 1
#do_text 007t 2 Auto WarteMal_t
do_action walk kiffen2 0
#sq_camera fix kamera4 0.9 -0.05 -0.6
#do_action rotate 0 2
#do_action anim tired 2
#do_text 007u 2 Auto WeihrauchIst_u
#do_text 007v 2 Auto HiHi_v

do_text 007w 2 Auto 007w
sq_camera fix kamera4 0.8 -0.05 -0.8
do_text 007x 2 Auto 007x
sq_camera fix kamera4 1.2 -0.25 -0.5
do_text 007y 2 Auto 007y
sq_camera fix 0 0.75 -0.1 0.8
sq_actor actionlist 0 {}
sq_wait 0
do_action anim scratchhead 0
do_text 007af 0 PosReac 007af
sq_camera move kamera4 1.5 -0.05 0.4 0.3
sq_actor actionlist 1 {}
sq_actor express 1 good_normal
do_action anim standup 1
do_wait time 3.5
do_action rotate front 1
+do_action beam kiffen2 0
+do_action beam kiffen1 2
+do_action beam Endpunkt 1
sq_camera get
+sq_object delete {0 1}
do_wait

#do_change muetze sparetime 1 ab
#do_change muetze sparetime 2 ab

#---inserted-by-Jan---MUSIC---------------
+adaptive_sound markerenable
+adaptive_sound changethemenow cave
#-----------------------------------------




