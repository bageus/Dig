show_loading yes
obj_clear
map create 300 300
sel /obj

set_view 32.2 32.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)
//set_fow_begin 300
//set_light_begin 300

set i [qnew Zwerg]
set_pos $i {157 78.5 13}
set i [qnew Zwerg]
set_pos $i {158 78.5 13}
set i [qnew Zwerg]
set_pos $i {159 78.5 13}
set i [qnew Zwerg]
set_pos $i {160 78.5 13}

//mat 

call templates/lava_unq_4thring.tcl
MapTemplateSet 32 32

call templates/lava_hol_025_a.tcl
MapTemplateSet 144 72

set_view 160 78 1

show_loading no

//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				{32	32	lava_unq_4thring}
				{144	72	lava_hol_025_a}
				{mat }
				}
