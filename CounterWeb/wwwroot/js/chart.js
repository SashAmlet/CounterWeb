function drawChart(id) {
    $.get('/api/Chart/GetGrades?id=' + id, function (GetGrades) {
        data = google.visualization.arrayToDataTable(GetGrades, false);
        var option = {
            title: "Успішність учнів курсу відповідно до балів за завдання (кількість оцінених у відповідний діапазон робіт)",
            width: 500,
            height: 400
        };
        chart = new google.visualization.PieChart(document.getElementById('chart1'));
        chart.draw(data, option);
        
    }).fail(function (jqXHR, textStatus, errorThrown) {
        // Код обработки ошибки
        console.log("Помилка запита: " + textStatus, errorThrown);
    });
}

function drawBarChart(id, studentsEncoded) {
    $.get('/api/Chart/GetStudents?id=' + id + '&students=' + studentsEncoded, function (GetStudents) {
        data = google.visualization.arrayToDataTable(GetStudents, false);
        var options = {
            title: "Успішність учнів курсу (середній бал в одиничному еквіваленті)",
            width: 500,
            height: 400,
        };


        var chart = new google.visualization.ColumnChart(document.getElementById("chart2"));
        chart.draw(data, options);
    }).fail(function (jqXHR, textStatus, errorThrown) {
        // Код обработки ошибки
        console.log("Помилка запита " + textStatus, errorThrown);
    });
}
