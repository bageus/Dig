show_loading yes
obj_clear
map create 300 300

;# create dummy object !!! don't touch it !!!
sel /obj


set_view 16.2 42.1 1.5 -0.3 0.0		;# set inital camera view (x y zoom)

//mat

call templates/swf_unq_drache.tcl
MapTemplateSet 4 36

call templates/swf_unq_tit_bug.tcl
MapTemplateSet 36 24

show_loading no
set_fow_begin 200

proc machihn {} {
    set i [qnew Zwerg]
    set_pos $i {16.125 42.5 13.0}
    add_expattrib $i exp_Stein 0.9
    add_expattrib $i exp_F_Kungfu 0.5
    add_expattrib $i exp_F_Defense 0.5
    add_expattrib $i exp_F_Sword 1
    set j [qnew Grosse_Holzkiepe]
    inv_add $i $j
    set j [qnew Taucherglocke]
    inv_add $i $j
    set j [qnew Schwert]
    inv_add $i $j
    log "Zack, da isser !"
	call_method $i Editor_Set_Info {{name Sascha} {gender male}}
	call_method 4i init

}

machihn
//#stopif NOT
//#stopifnot NOT
sm_def_temp_group temp_name {
				{4	36	swf_unq_drache}
				{36	24	swf_unq_tit_bug}
				{88	24	swf_unq_tit_masch}
				{132	32	swf_unq_tit_treppe}
				}


