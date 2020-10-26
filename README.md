# SRISC Assembler Spec

### Mnemonics

```
imm		target[0..3]	value[d0..255]
						value[h0..ff]
						value[b.....]
add		target[0..3]	a[0..3]		b[0..3]
sub		target[0..3]	a[0..3]		b[0..3]
and		target[0..3]	a[0..3]		b[0..3]
or		target[0..3]	a[0..3]		b[0..3]
xor		target[0..3]	a[0..3]		b[0..3]
inv		target[0..3]	a[0..3]
sll		target[0..3]	a[0..3]		b[0..3]
srl		target[0..3]	a[0..3]		b[0..3]
inc		target[0..3]	a[0..3]
dec		target[0..3]	a[0..3]
ind		a[0..3]
ult		a[0..3]		b[0..3]
slt		a[0..3]		b[0..3]
equ		a[0..3]		b[0..3]
eqz		a[0..3]
set
load	target[0..3]	addr[0..110]
load	target[0..3]
store	source[0..3]	addr[0..110]
store	source[0..3]
read	target[0..3]	addr[0..15]
write	source[0..3]	addr[0..15]
br		addr[0..1023]
```
		
## Compilation

### Lexicon

```
<mnemonics>
<type>
	byte
	short
	int
	long
	
if
for
while
```


#### Variables (byte)
* variable dictionary
* swaps most decrepit register
* init done first, optional assign after
##### Syntax
```
		byte a;
			try add 'a' to dict
			no code
		byte a = #
			try add 'a' to dict
			dreg <= imm #
		byte a = b
			try add 'a' to dict
			dreg <= imm value of b (b stays put)
		byte a = @ #
			try add 'a' to dict
			dreg "temp" <= imm
			"temp" <= alu "temp"
		byte a = @ b
			try add 'a' to dict
			dreg "temp" <= value of b
			"temp" <= alu "temp"
		byte a = # @ #
			try add 'a' to dict
			? dreg <= imm of #, #
			dreg <= alu #, #
		byte a = b @ #
			try add 'a' to dict
			? dreg <= value of b, #
			dreg <= alu b, #
		byte a = b @ c
			try add 'a' to dict
			? dreg <= value of b, c
			dreg <= alu b, c
```	
##### Procedure
* Init - breaks syntax into init and then assignment
```
      read type, name
      if name exists
          error
      else
          add var to dict (inactive)
      end		
      if '='
          doAssignment(line.remove(type))
      end
```
* Existing/Assignment
```
      read name
      if exists			
          if op1 is #
              imm #
          else if op1 exists
              imm value
          else
              error - var not found
          end
          if '@'
          symbol = '@'
              if op2 is #
                  imm #
              else if op2 exists
                  imm value
              else
                  error - var not found
              end
              alu op
          end				
      else
          error
      end
```

#### Ifs
* code inside placed elsewhere
* true branches to code and jumps back
* false continues in place
##### Syntax
```
		if (a ? #/b)
		end
```
		
##### Procedure
```
      if ? is '=' and #/b is 0
      else
          if #/b is #
              imm #
          else if op2 exists
              imm value
          else
              error - var not found
          end		
      end
      compare
      branch?
      run
      return
      delete vars inside
```

#### For
* code inside placed elsewhere
* immediately jump to conditional
* true branches to top of loop
* false continues to jump back
##### Syntax
```
		for (<type> a = #/b; a ? #/c; a @ #/d)
		end
```	
  ##### Procedure
```
      init? a
      imm #/b
      jump to conditional
          compare
          branch? to top of loop
          run
          imm #/d
          modify a
      jump back
```

#### While
* code inside placed elsewhere
* immediately jump to conditional
* true branches to top of loop
* false continues to jump back
##### Syntax
```
		while (a @ #/b)
		end
```	
##### Procedure
```
      imm a
      imm #/b
      jump to conditional
          compare
          branch? to top of loop
          run
      jump back
```

#### Arrays
	shorts/ints/longs*
		stored big endian
	strings*
	linked lists*
	
### Ideas
* registers cache memory caches SD card
* self-reprogram from SD card
* run linux