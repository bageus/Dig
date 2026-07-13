if {[in_class_def]} {
	log "Don't call File_Wrapper.tcl in class definitions !"
} else {

	proc static_set {VarName Value} {
		upvar $VarName $VarName
		if { ![info exists $VarName] } {
			set $VarName $Value
		}
	}

	proc check_file_handle {FileHandle} {
		global $FileHandle
		if { ![info exists $FileHandle] } {
			return 0
		}
		return 1
	}

	proc get_seek_varname {FileHandle} {
		return [string map {_File_ _FileSeek_} $FileHandle]
	}

	proc get_length_varname {FileHandle} {
		return [string map {_File_ _FileLength_} $FileHandle]
	}

	proc open {FileName {Access "r"} {Permissions "ignored"}} {

		set cached 1
//#ifdef SQ_NOCACHE
		set cached 0
//#endif
		if { $Access == "nc" } {
			set cached 0
		}

		if { [catch { set FileData [load_file $FileName $cached] }] } {
			log "File not found: $FileName"
			return 0
		}

		global __file_cnt
		static_set __file_cnt 0

		set pF 			"FW_File_$__file_cnt"
		set pFSeek  	"FW_FileSeek_$__file_cnt"
		set pFLength	"FW_FileLength_$__file_cnt"
		global $pFSeek $pFLength $pF

		set $pF [split $FileData "\n"]
		set $pFSeek 0
		set $pFLength [llength [subst $$pF]]

		incr __file_cnt

		return $pF
	}


	proc close { FileHandle } {
		global $FileHandle
		if { ![check_file_handle $FileHandle] } {
			log "Invalid file handle: $FileHandle"
			return 0
		}

		global __file_cnt
		unset $FileHandle
		set SF [get_seek_varname $FileHandle]
		set LF [get_length_varname $FileHandle]
		global $SF $LF
		unset $SF
		unset $LF
		incr __file_cnt -1

		return 1
	}


    proc gets {FileHandle {VarName "__invalid"}} {
    	global $FileHandle
    	if {![check_file_handle $FileHandle]} {
			log "Invalid file handle: $FileHandle"
			return 0
    	}

    	set FL [get_length_varname $FileHandle]
    	set FS [get_seek_varname $FileHandle]
    	global $FL $FS $FileHandle

    	set SIdx [subst $$FS]
    	set RetVal [lindex [subst $$FileHandle] $SIdx]

    	incr $FS

    	if { $VarName == "__invalid" } {
    		return $RetVal
    	} else {
    		upvar $VarName $VarName
    		set $VarName $RetVal
    	}
    }

    proc eof { FileHandle } {
    	global $FileHandle
    	set FL [get_length_varname $FileHandle]
    	set FS [get_seek_varname $FileHandle]
    	global $FS $FL
    	if { [subst $$FS] >= [subst $$FL] } {
    		return 1
    	}
    	return 0
    }







}
