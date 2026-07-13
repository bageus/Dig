proc repn {count body } {
	for {set i 0} {$i<$count} {incr i} {
		uplevel 2 $body
	}
}
