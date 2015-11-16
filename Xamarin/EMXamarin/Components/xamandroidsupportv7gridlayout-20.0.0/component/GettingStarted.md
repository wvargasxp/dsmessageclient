v7 Support GridLayout Library
=========================

There are several libraries designed to be used with Android 2.1 (API level 7) and higher. 
These libraries provide specific feature sets and can be included in your application independently 
from each other.  

This library adds support for the [GridLayout][4] class, which allows you to arrange user interface elements 
using a grid of rectangular cells. For detailed information about the v7 gridlayout library APIs, see 
the [android.support.v7.widget][5] package in the API reference.

Using GridLayout
------

#####grid_layout_1.xml
```xml
<?xml version="1.0" encoding="utf-8"?>
<android.support.v7.widget.GridLayout xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    android:layout_width="match_parent"
    android:layout_height="wrap_content"
    android:background="@drawable/blue"
    android:padding="10dip"
    app:columnCount="4">
    <TextView
        android:text="@string/grid_layout_1_instructions" />
    <EditText
        app:layout_gravity="fill_horizontal"
        app:layout_column="0"
        app:layout_columnSpan="4" />
    <Button
        android:text="@string/grid_layout_1_cancel"
        app:layout_column="2" />
    <Button
        android:text="@string/grid_layout_1_ok"
        android:layout_marginLeft="10dip" />
</android.support.v7.widget.GridLayout>
```
#####GridLayout1.cs
```csharp
[Activity (Label = "@string/grid_layout_1")]
public class GridLayout1 : Activity
{
	protected override void OnCreate (Bundle bundle)
	{
		base.OnCreate (bundle);
		SetContentView(Resource.Layout.grid_layout_1);
	}
}
```

*Portions of this page are modifications based on [work][3] created and [shared by the Android Open Source Project][1] and used according to terms described in the [Creative Commons 2.5 Attribution License][2].*

[1]: http://code.google.com/policies.html
[2]: http://creativecommons.org/licenses/by/2.5/
[3]: http://developer.android.com/tools/support-library/features.html
[4]: http://developer.android.com/reference/android/support/v7/widget/GridLayout.html
[5]: http://developer.android.com/reference/android/support/v7/widget/package-summary.html
