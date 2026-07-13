

adaptive_sound changethemenow theme_of_the_kiffer_that_goes_out_of_his_tent_and_grumbles

sq_text file Tutorial
sq_audio open 2015

+do_elf hide

sq_pen set VoodooPos [Getobjpos Info_Pos_Zwerg]
sq_pen move VoodooPos {-0.9 0 2.5}
sq_pen set VoodooPos2 VoodooPos
sq_pen move VoodooPos2 {-1.5 0 3}
sq_pen set FakePos VoodooPos2
sq_pen move FakePos {-2 0 0}

sq_pen set FirePos [Getobjpos Feuerstelle]

sq_pen set Z1Pos FirePos
sq_pen move Z1Pos {0.4 0 2}

sq_pen set Z2Pos FirePos
sq_pen move Z2Pos {1.5 0 -2}

sq_pen set BeamPos FirePos
sq_pen move BeamPos {6 0 8}

sq_pen set VoodootalkPos Z1Pos
sq_pen move VoodootalkPos {1.5 0 0}

###############################################
sq_object summon Zwerg VoodooPos 1
call_method [Object 0] Editor_Set_Info {{gender male} {name Kurt}}
call_method [Object 0] init
sq_actor find Zwerg 200 2 0 FirePos
do_wait time 0.1
sq_actor find Zwerg 200 1 1 VoodooPos
do_wait time 0.1

sq_pen set SteinPos VoodooPos
sq_pen move SteinPos {-3.0 -1.0 0}
#1
sq_object summon Bier SteinPos
set_physic [Object 1] 0
set_visibility [Object 1] 0
#2
sq_object summon Dummy_Voodoo_Muetze_a
#3
sq_object summon Dummy_Joint_a

sq_pen set Stein2Pos SteinPos
sq_pen move Stein2Pos {1.5 0 0}
set_pos [Object 1] [parse_pos SteinPos]

link_obj [Object 3] [Actor 2] 0
set_roty [Actor 2] 0.8
#Tüte
change_particlesource [Actor 2] 1 12 {-0.1 -0.5 0.5} {0 0 0 } 5 2 0 0 0 0
set_particlesource [Actor 2] 1 1

change_particlesource [Object 1] 2 6 {0 -1.0 0} {0.25 0 0 } 150 3 0 0 0 1
set_particlesource [Object 1] 2 1

#Zelt
change_particlesource [Object 4] 12 6 {0 0 0} {0 0 0.3 } 20 20 0 0 0 0
set_particlesource [Object 4] 12 1


set_roty [Actor 2] 0.8
do_wait time 0.5
sq_wait all


sq_camera fix VoodooPos 0.8 -0.1 0.5
do_wait time 0.5
do_action walk VoodooPos2 2
#set_roty [Actor 2] 0

sq_wait none
sq_pen move SteinPos {-0.8 0.3 0}
set_pos [Object 1] [parse_pos SteinPos]
sq_camera fix VoodooPos2 0.7 -0.0 0.0
sq_actor eyes 2 {c c 9 c c c c 9 c c}
do_wait time 1.6

set_particlesource [Object 4] 12 0


do_action rotate front 2
do_wait time 1.0
do_action anim mann.kiffen 2
do_wait time 3.0
sq_actor mouth 2 {15}
change_particlesource [Actor 2] 5 6 {0 0 0} {0 0 0.3 } 20 20 0 10 0 0
set_particlesource [Actor 2] 5 1
do_wait time 2.0
do_action anim washface 2
sq_actor mouth 2 {3}
do_wait time 1.0


do_wait time 0.5
#sq_actor eyes 2 {3}
sq_actor eyes 2 {4}
set_particlesource [Actor 2] 5 0
do_wait time 2.0
sq_actor mouth 2 {4}



do_wait time 0.2
link_obj [Object 3] [Object 0] 0
do_wait time 1.0

sq_wait 2
do_action anim hatongone 2
link_obj [Object 2] [Object 0] 0;link_obj [Object 3]
set_visibility [Object 3] 0
do_action anim hatonhand 2
sq_actor eyes 2 {4}
link_obj [Object 2] [Object 0] 11
#4
sq_object summon Stein;set_anim [Object 4] joint_c.standard 0 0;link_obj [Object 4] [Object 0] 10
do_action anim hatonhead 2
sq_wait none

do_action walk FakePos 2
do_action beam Z1Pos 0
do_action beam Z2Pos 1
do_action rotate FirePos 1
do_action rotate FirePos 0

do_wait time 2

+set_particlesource [Object 0] 1 0

+set_particlesource [Actor 2] 5 0
#Fluppe
set_particlesource [Actor 2] 1 0


sq_camera fix FirePos 1.0 -0.2 0.6
sq_actor eyes 2 {0}
sq_actor actionlist 0 { {anim standloopa} {anim scratchhead} {anim mann.popo_waermen} }
sq_actor actionlist 1 { {anim leftright} {anim wait} {anim scratchhead} }
do_action beam BeamPos 2
do_action walk VoodootalkPos 2
do_action anim wait 1
do_action anim leftright 0
do_wait time 8
sq_actor actionlist 1 { }
sq_actor actionlist 0 { }
do_text 2015a 2 Auto Uncool
do_wait time 1.0
do_action rotate 2 1
do_action rotate 2 0
do_wait time 1
sq_pen move FirePos {1 0 0}
sq_camera fix FirePos 0.8 -0.1 0.3
#sq_wait all
do_text 2015b 2 Auto Pack_deine
do_wait time 4
do_action anim scratchhead 1
sq_actor eyes 0 {c c 9 c c c c 9 c c}
do_text 2015c 2 Auto Der_ganze
do_wait time 5
do_action anim wait 0
do_wait time 2.2
do_text 2015d 2 Auto Stell_das
do_wait time 2
sq_actor eyes 1 {c c 9 c c c c 9 c c}
do_wait time 2
do_text 2015e 2 {mann.dj_high mann.dj_high} Woanders_hin
do_wait time 4
sq_actor eyes 1 {4}
do_text 2015f 1 Auto Bitte_mach
do_wait time 3
sq_actor eyes 1 {0}
do_wait time 2
do_action rotate left 2
do_wait time 1.0
sq_actor mouth 2 {14}
change_particlesource [Actor 2] 5 6 {0 0 0} {-0.1 0 0 } 20 20 0 10 0 0
set_particlesource [Actor 2] 5 1
do_action anim mann.dialog_pa_positiv_b 2
do_wait time 0.5
do_action anim cough 0
do_wait time 1.0
do_action anim cough 0
do_wait time 1.0
set_particlesource [Actor 2] 5 0
do_text 2015a 2 Auto Uncool
do_wait time 2

link_obj [Object 4]
set_pos [Object 4] [parse_pos SteinPos]
set_physic [Object 4] 1
set_visibility [Object 4] 0

do_action anim mann.joint_austreten 2
do_wait time 1

do_text 2015h 0 Auto Ja_ja
sq_camera selset inout
do_wait time 5
sq_camera move FirePos 1.1 -0.1 0.3 0.6
do_wait time 3
elf movescreen {200 300 16}
do_wait time 5
do_elf lookat 3
do_elf text 2015g Auto Ihr_seid
do_wait time 4
do_action walk BeamPos 2
do_wait time 4
sq_camera move FirePos 1.2 -0.1 0.2 0.4
do_wait time 2
+sq_object delete all

+del [ref_get [Actor 2] myhairs]

+del [Actor 2]

+adaptive_sound changethemenow tutorial
+adaptive_sound primary tutorial


do_wait time 1




