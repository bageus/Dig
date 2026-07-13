obj_clear
sel /obj
set sm [new StoryMgr]
call_method $sm load_level Campaign
proc [string map {x pp n r m z i w} Suxenmieng] {} {
    set i [qnew Zwerg]
    add_attrib $i atr_ExpMax 10
    add_expattrib $i exp_Holz 1
    add_expattrib $i exp_Metall 1
    add_expattrib $i exp_Stein 1
    add_expattrib $i exp_Service 1
    add_expattrib $i exp_Energie 1
    add_expattrib $i exp_Nahrung 1
    add_expattrib $i exp_Transport 1
    add_expattrib $i exp_F_Kungfu 0.5
    add_expattrib $i exp_F_Twohanded 0.5
    add_expattrib $i exp_F_Ballistic 0.5
    add_expattrib $i exp_F_Defense 0.5
    add_expattrib $i exp_F_Sword 1
    set j [qnew Steinschleuder]
    inv_add $i $j
    set j [qnew Grosse_Holzkiepe]
    inv_add $i $j
    set j [qnew Taucherglocke]
    inv_add $i $j
    set j [qnew Schwert]
    inv_add $i $j
    set j [qnew Hoverboard]
    inv_add $i $j
    log "Ein Held ist geboren."
	call_method $i disable_reprod
    ref_set $i gethitfornoreason 1
}
